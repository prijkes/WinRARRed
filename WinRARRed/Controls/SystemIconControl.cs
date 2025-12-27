using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace WinRARRed.Controls
{
    public abstract class SystemIconControl : Control
    {
        private Icon Icon { get; set; }

        public SystemIconControl(Icon icon, SystemSound? sound = null)
        {
            Icon = icon;

            Width = Icon.Width;
            Height = Icon.Height;

            sound?.Play();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle rect = new(0, 0, Width, Height);
            e.Graphics.DrawIcon(Icon, rect);
        }
    }
}
