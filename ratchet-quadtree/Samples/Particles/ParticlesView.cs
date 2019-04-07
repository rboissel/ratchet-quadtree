using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Particles
{
    public partial class ParticlesView : Form
    {
        System.Drawing.Bitmap bitmap = new Bitmap(256, 256);
        Particles particles = new Particles(256, 2000);
        public ParticlesView()
        {
            InitializeComponent();
        }


        private void ParticlesView_Load(object sender, EventArgs e)
        {

        }
        bool _DrawTree = false;

        private void ParticlesView_Paint(object sender, PaintEventArgs e)
        {
            System.Drawing.Pen pen = new System.Drawing.Pen(Color.FromArgb(64, 255, 255, 255));
            particles.Draw(bitmap, _DrawTree);

            if (_DrawTree)
            {
                var g = System.Drawing.Graphics.FromImage(bitmap);
                Ratchet.Collections.Quadtree<Particles.AgregatedParticle> tree = particles.CreateGrid();
                DateTime requestStart = DateTime.Now;

                foreach (var node in tree.Query(0, 0, (ulong)bitmap.Width, (ulong)bitmap.Height))
                {
                    if (node.Size > 1) { g.DrawRectangle(pen, node.X, node.Y, node.Size, node.Size); }
                }
                DateTime requestEnd = DateTime.Now;
                Console.WriteLine((requestEnd - requestStart).TotalMilliseconds);
            }

            e.Graphics.DrawImage(bitmap, new Rectangle(0, 0, Width, Height));

        }

        private void refresh_Tick(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _DrawTree = checkBox1.Checked;
        }
    }
}
