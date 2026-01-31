using System.Windows.Forms;

namespace WinRARRed.Forms;

public partial class RARChecksumNotFoundForm : Form
{
        public RARChecksumNotFoundForm()
        {
            InitializeComponent();

            btnOK.Click += BtnOK_Click;
        }

        private void BtnOK_Click(object? sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
    }
}
