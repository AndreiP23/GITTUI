using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace GITTUI.Views
{
    internal static class CreateWorkflowDialog
    {
        public static void Show(string repoName, Action<string, string> onCommit)
        {
            var dialog = new Dialog($" Create Workflow — {repoName} ", 90, 30)
            {
                ColorScheme = new ColorScheme
                {
                    Normal = Attribute.Make(Color.White, Color.Black),
                    Focus = Attribute.Make(Color.BrightYellow, Color.Black),
                    HotNormal = Attribute.Make(Color.BrightCyan, Color.Black),
                    HotFocus = Attribute.Make(Color.BrightCyan, Color.DarkGray),
                }
            };

            var fileLabel = new Label("Filename:") { X = 1, Y = 0 };
            var fileField = new TextField("ci.yml")
            {
                X = 12,
                Y = 0,
                Width = Dim.Fill(1)
            };

            var editorLabel = new Label("YAML:") { X = 1, Y = 2 };
            var editor = new TextView
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1),
                Height = Dim.Fill(3),
                ReadOnly = false,
                CanFocus = true,
                WordWrap = false,
                Text = DefaultSkeleton(),
                ColorScheme = new ColorScheme
                {
                    Normal = Attribute.Make(Color.BrightGreen, Color.Black),
                    Focus = Attribute.Make(Color.BrightGreen, Color.Black),
                    HotNormal = Attribute.Make(Color.BrightGreen, Color.Black),
                    HotFocus = Attribute.Make(Color.BrightGreen, Color.Black),
                }
            };

            var commitButton = new Button("Commit");
            commitButton.Clicked += () =>
            {
                var fileName = fileField.Text?.ToString()?.Trim() ?? "";
                var yaml = editor.Text?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    MessageBox.ErrorQuery("Error", "Filename cannot be empty.", "Ok");
                    return;
                }

                if (!fileName.EndsWith(".yml") && !fileName.EndsWith(".yaml"))
                    fileName += ".yml";

                if (string.IsNullOrWhiteSpace(yaml))
                {
                    MessageBox.ErrorQuery("Error", "Workflow content cannot be empty.", "Ok");
                    return;
                }

                Application.RequestStop();
                onCommit(fileName, yaml);
            };

            var cancelButton = new Button("Cancel", true);
            cancelButton.Clicked += () => Application.RequestStop();

            dialog.Add(fileLabel, fileField, editorLabel, editor);
            dialog.AddButton(commitButton);
            dialog.AddButton(cancelButton);

            Application.Run(dialog);
        }

        private static string DefaultSkeleton()
        {
            return
                "name: CI\n" +
                "\n" +
                "on:\n" +
                "  push:\n" +
                "    branches: [ main ]\n" +
                "  pull_request:\n" +
                "    branches: [ main ]\n" +
                "\n" +
                "jobs:\n" +
                "  build:\n" +
                "    runs-on: ubuntu-latest\n" +
                "    steps:\n" +
                "      - uses: actions/checkout@v4\n" +
                "      - name: Build\n" +
                "        run: echo \"Hello, World!\"\n";
        }
    }
}
