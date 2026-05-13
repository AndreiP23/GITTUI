using GITTUI.Services;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal static class SettingsDialog
    {
        public static void Show(RuntimeSettings current, Action<RuntimeSettings> onSave)
        {
            var dialog = new Dialog(" Settings ", 70, 20)
            {
                ColorScheme = new ColorScheme
                {
                    Normal = Attribute.Make(Color.White, Color.Black),
                    Focus = Attribute.Make(Color.BrightYellow, Color.Black),
                    HotNormal = Attribute.Make(Color.BrightCyan, Color.Black),
                    HotFocus = Attribute.Make(Color.BrightCyan, Color.DarkGray),
                }
            };

            var historyDaysField = CreateTextField(current.HistoryDays.ToString(), 24, 1);
            var debounceField = CreateTextField(current.DebounceDelayMs.ToString(), 24, 3);
            var reposTtlField = CreateTextField(current.RepositoriesTtlSeconds.ToString(), 24, 5);
            var activityTtlField = CreateTextField(current.ActivityTtlSeconds.ToString(), 24, 7);
            var autoRefreshCheckBox = new CheckBox(24, 9, "Enabled", current.AutoRefreshEnabled);
            var autoRefreshIntervalField = CreateTextField(current.AutoRefreshIntervalSeconds.ToString(), 24, 11);

            dialog.Add(
                new Label("History Days:") { X = 1, Y = 1 },
                historyDaysField,
                new Label("Debounce (ms):") { X = 1, Y = 3 },
                debounceField,
                new Label("Repos TTL (s):") { X = 1, Y = 5 },
                reposTtlField,
                new Label("Activity TTL (s):") { X = 1, Y = 7 },
                activityTtlField,
                new Label("Auto Refresh:") { X = 1, Y = 9 },
                autoRefreshCheckBox,
                new Label("Refresh Interval (s):") { X = 1, Y = 11 },
                autoRefreshIntervalField);

            var saveButton = new Button("Save");
            saveButton.Clicked += () =>
            {
                if (!TryParsePositiveInt(historyDaysField.Text, "History Days", out var historyDays)
                    || !TryParseNonNegativeInt(debounceField.Text, "Debounce", out var debounceDelayMs)
                    || !TryParsePositiveInt(reposTtlField.Text, "Repositories TTL", out var repositoriesTtlSeconds)
                    || !TryParsePositiveInt(activityTtlField.Text, "Activity TTL", out var activityTtlSeconds)
                    || !TryParseNonNegativeInt(autoRefreshIntervalField.Text, "Refresh Interval", out var autoRefreshIntervalSeconds))
                {
                    return;
                }

                Application.RequestStop();
                onSave(new RuntimeSettings(
                    HistoryDays: historyDays,
                    DebounceDelayMs: debounceDelayMs,
                    RepositoriesTtlSeconds: repositoriesTtlSeconds,
                    ActivityTtlSeconds: activityTtlSeconds,
                    AutoRefreshEnabled: autoRefreshCheckBox.Checked,
                    AutoRefreshIntervalSeconds: autoRefreshIntervalSeconds));
            };

            var cancelButton = new Button("Cancel", true);
            cancelButton.Clicked += () => Application.RequestStop();

            dialog.AddButton(saveButton);
            dialog.AddButton(cancelButton);

            Application.Run(dialog);
        }

        private static TextField CreateTextField(string initialValue, int x, int y)
        {
            return new TextField(initialValue)
            {
                X = x,
                Y = y,
                Width = 10
            };
        }

        private static bool TryParsePositiveInt(object? value, string label, out int result)
        {
            if (int.TryParse(value?.ToString(), out result) && result > 0)
                return true;

            MessageBox.ErrorQuery("Invalid value", $"{label} must be a positive integer.", "Ok");
            result = 0;
            return false;
        }

        private static bool TryParseNonNegativeInt(object? value, string label, out int result)
        {
            if (int.TryParse(value?.ToString(), out result) && result >= 0)
                return true;

            MessageBox.ErrorQuery("Invalid value", $"{label} must be zero or a positive integer.", "Ok");
            result = 0;
            return false;
        }
    }
}