using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk
{
    public partial class Drawer : Form
    {
        public Drawer()
        {
            InitializeComponent();
        }
        public Bitmap Image
        {
            set
            {
                pictureBox1.Image = value;
            }
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
