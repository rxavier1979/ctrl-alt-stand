using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CtrlAltStand
{
    internal enum DeskPhase
    {
        Sit,
        Stand,
        Move
    }

    // Schedule model — identical semantics to the original WinForms app.
    internal sealed class CyclePlan
    {
        public int SitMinutes = 30;
        public int StandMinutes = 20;
        public int MoveMinutes = 3;
        public bool MoveEnabled = true;
        public DeskPhase StartPhase = DeskPhase.Sit;

        public int SecondsFor(DeskPhase phase)
        {
            switch (phase)
            {
                case DeskPhase.Stand:
                    return StandMinutes * 60;
                case DeskPhase.Move:
                    return MoveMinutes * 60;
                default:
                    return SitMinutes * 60;
            }
        }

        public DeskPhase Next(DeskPhase current)
        {
            if (current == DeskPhase.Sit)
            {
                return DeskPhase.Stand;
            }

            if (current == DeskPhase.Stand && MoveEnabled)
            {
                return DeskPhase.Move;
            }

            return DeskPhase.Sit;
        }

        public CyclePlan Clone()
        {
            CyclePlan copy = new CyclePlan();
            copy.SitMinutes = SitMinutes;
            copy.StandMinutes = StandMinutes;
            copy.MoveMinutes = MoveMinutes;
            copy.MoveEnabled = MoveEnabled;
            copy.StartPhase = StartPhase;
            return copy;
        }
    }

    internal sealed class AppSettings
    {
        public readonly CyclePlan Plan = new CyclePlan();
        public CyclePlan Profile1;
        public CyclePlan Profile2;
        public bool SoundEnabled = true;
        public bool AlwaysOnTop = true;

        private static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CtrlAltStand",
                    "settings.ini");
            }
        }

        public void Load()
        {
            if (!File.Exists(SettingsPath))
            {
                return;
            }

            foreach (string rawLine in File.ReadAllLines(SettingsPath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator < 1)
                {
                    continue;
                }

                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                int parsedNumber;
                bool parsedBoolean;

                if (key.StartsWith("Profile1.", StringComparison.OrdinalIgnoreCase))
                {
                    LoadProfileValue(1, key.Substring(9), value);
                }
                else if (key.StartsWith("Profile2.", StringComparison.OrdinalIgnoreCase))
                {
                    LoadProfileValue(2, key.Substring(9), value);
                }
                else if (key == "SitMinutes" && int.TryParse(value, out parsedNumber))
                {
                    Plan.SitMinutes = ClampMinutes(parsedNumber);
                }
                else if (key == "StandMinutes" && int.TryParse(value, out parsedNumber))
                {
                    Plan.StandMinutes = ClampMinutes(parsedNumber);
                }
                else if (key == "MoveMinutes" && int.TryParse(value, out parsedNumber))
                {
                    Plan.MoveMinutes = ClampMinutes(parsedNumber);
                }
                else if (key == "MoveEnabled" && bool.TryParse(value, out parsedBoolean))
                {
                    Plan.MoveEnabled = parsedBoolean;
                }
                else if (key == "StartPhase")
                {
                    Plan.StartPhase = string.Equals(value, "Stand", StringComparison.OrdinalIgnoreCase)
                        ? DeskPhase.Stand
                        : DeskPhase.Sit;
                }
                else if (key == "SoundEnabled" && bool.TryParse(value, out parsedBoolean))
                {
                    SoundEnabled = parsedBoolean;
                }
                else if (key == "AlwaysOnTop" && bool.TryParse(value, out parsedBoolean))
                {
                    AlwaysOnTop = parsedBoolean;
                }
            }
        }

        public void Save()
        {
            string directory = Path.GetDirectoryName(SettingsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            List<string> lines = new List<string>
            {
                "SitMinutes=" + Plan.SitMinutes,
                "StandMinutes=" + Plan.StandMinutes,
                "MoveMinutes=" + Plan.MoveMinutes,
                "MoveEnabled=" + Plan.MoveEnabled,
                "StartPhase=" + Plan.StartPhase,
                "SoundEnabled=" + SoundEnabled,
                "AlwaysOnTop=" + AlwaysOnTop
            };

            AppendProfile(lines, "Profile1", Profile1);
            AppendProfile(lines, "Profile2", Profile2);
            File.WriteAllLines(SettingsPath, lines.ToArray());
        }

        public CyclePlan GetProfile(int slot)
        {
            return slot == 1 ? Profile1 : Profile2;
        }

        public void SaveProfile(int slot)
        {
            if (slot == 1)
            {
                Profile1 = Plan.Clone();
            }
            else
            {
                Profile2 = Plan.Clone();
            }
        }

        private void LoadProfileValue(int slot, string field, string value)
        {
            CyclePlan profile = slot == 1 ? Profile1 : Profile2;
            if (profile == null)
            {
                profile = new CyclePlan();
                if (slot == 1)
                {
                    Profile1 = profile;
                }
                else
                {
                    Profile2 = profile;
                }
            }

            int parsedNumber;
            bool parsedBoolean;
            if (field == "SitMinutes" && int.TryParse(value, out parsedNumber))
            {
                profile.SitMinutes = ClampMinutes(parsedNumber);
            }
            else if (field == "StandMinutes" && int.TryParse(value, out parsedNumber))
            {
                profile.StandMinutes = ClampMinutes(parsedNumber);
            }
            else if (field == "MoveMinutes" && int.TryParse(value, out parsedNumber))
            {
                profile.MoveMinutes = ClampMinutes(parsedNumber);
            }
            else if (field == "MoveEnabled" && bool.TryParse(value, out parsedBoolean))
            {
                profile.MoveEnabled = parsedBoolean;
            }
            else if (field == "StartPhase")
            {
                profile.StartPhase = string.Equals(value, "Stand", StringComparison.OrdinalIgnoreCase)
                    ? DeskPhase.Stand
                    : DeskPhase.Sit;
            }
        }

        private static void AppendProfile(List<string> lines, string prefix, CyclePlan profile)
        {
            if (profile == null)
            {
                return;
            }

            lines.Add(prefix + ".SitMinutes=" + profile.SitMinutes);
            lines.Add(prefix + ".StandMinutes=" + profile.StandMinutes);
            lines.Add(prefix + ".MoveMinutes=" + profile.MoveMinutes);
            lines.Add(prefix + ".MoveEnabled=" + profile.MoveEnabled);
            lines.Add(prefix + ".StartPhase=" + profile.StartPhase);
        }

        private static int ClampMinutes(int value)
        {
            return Math.Max(1, Math.Min(180, value));
        }
    }

    internal static class SelfTests
    {
        public static bool Run()
        {
            CyclePlan plan = new CyclePlan();
            if (plan.SecondsFor(DeskPhase.Sit) != 1800) return false;
            if (plan.SecondsFor(DeskPhase.Stand) != 1200) return false;
            if (plan.SecondsFor(DeskPhase.Move) != 180) return false;
            if (plan.Next(DeskPhase.Sit) != DeskPhase.Stand) return false;
            if (plan.Next(DeskPhase.Stand) != DeskPhase.Move) return false;
            if (plan.Next(DeskPhase.Move) != DeskPhase.Sit) return false;
            plan.MoveEnabled = false;
            if (plan.Next(DeskPhase.Stand) != DeskPhase.Sit) return false;
            plan.SitMinutes = 45;
            plan.StartPhase = DeskPhase.Stand;
            CyclePlan copy = plan.Clone();
            plan.SitMinutes = 10;
            if (copy.SitMinutes != 45) return false;
            if (copy.StartPhase != DeskPhase.Stand) return false;

            AppSettings memorySettings = new AppSettings();
            memorySettings.Plan.SitMinutes = 35;
            memorySettings.Plan.StandMinutes = 25;
            memorySettings.Plan.MoveMinutes = 5;
            memorySettings.Plan.MoveEnabled = true;
            memorySettings.Plan.StartPhase = DeskPhase.Stand;
            memorySettings.SaveProfile(1);
            memorySettings.Plan.SitMinutes = 15;
            if (memorySettings.Profile1 == null) return false;
            if (memorySettings.Profile1.SitMinutes != 35) return false;
            if (memorySettings.Profile1.StandMinutes != 25) return false;
            if (memorySettings.Profile1.MoveMinutes != 5) return false;
            if (!memorySettings.Profile1.MoveEnabled) return false;
            if (memorySettings.Profile1.StartPhase != DeskPhase.Stand) return false;
            return true;
        }

        // Formats seconds as MM:SS, or H:MM:SS when an hour or more remains.
        public static string FormatClock(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            return hours > 0
                ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", hours, minutes, seconds)
                : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes, seconds);
        }
    }
}
