using System.Windows.Forms;

namespace WinRARRed.Forms
{
    public partial class ModifiedDateWarningForm : Form
    {
        public ModifiedDateWarningForm()
        {
            InitializeComponent();

            btnOK.Click += BtnOK_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        private void BtnOK_Click(object? sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void BtnCancel_Click(object? sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
