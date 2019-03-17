using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Defogging
{
    public partial class Form1 : Form
    {
        private Image imgInitial;
        private Bitmap[] imgResult;
        private double[] rgbM;

        public Form1()
        {
            InitializeComponent();

            rgbM = new double[] { 1.0, 1.0, 1.0 };

            try
            {
                DirectoryInfo info = new DirectoryInfo(Application.StartupPath);
                imgInitial = Image.FromFile(info.Parent.FullName + "/src/1.jpg");
                pictureBox1.Image = imgInitial;
            }
            catch(Exception)
            {
                
            }
        }

        private void button_run_Click(object sender, EventArgs e)
        {
            var w = (double)trackBar_transmission.Value / 100;
            var p = (int)trackBar1.Value * 2 - 1;
            var q = (int)trackBar2.Value;
            Core.Adjust(w, p, q);

            var startTime = DateTime.Now;
            Image<Bgr, byte> source = new Image<Bgr, byte>((Bitmap)imgInitial);

            if (radioButton1.Checked)
            {
                imgResult = Core.Dehaze(source, 0);
                Console.WriteLine("softmatting");
            }
            else if (radioButton2.Checked)
            {
                Console.WriteLine("guidedfiltering");
                imgResult = Core.Dehaze(source, 1);
            }
            
            pictureBox2.Image = imgResult[0];
            pictureBox3.Image = imgResult[1];
            pictureBox4.Image = imgResult[2];
            var timeSpan = (DateTime.Now - startTime);
            textBox_runtime.Text = timeSpan.TotalSeconds.ToString().Substring(0, 5) + "s";
        }

        private void button_choose_file_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                DialogResult dr = dlg.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    string fileName = dlg.FileName;
                    imgInitial = Image.FromFile(fileName);
                    pictureBox1.Image = imgInitial;
                             
                }

            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Form2 showPic = new Form2(imgInitial);
            showPic.Show();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Form2 showPic = new Form2(imgResult[0]);
            showPic.Show();
        }

        private void trackBar_transmission_Scroll(object sender, EventArgs e)
        {
            textBox_transmission_v.Text = ((double)trackBar_transmission.Value).ToString() + "%";
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBox1.Text = ((int)trackBar1.Value * 2 - 1).ToString();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            textBox2.Text = ((int)trackBar2.Value).ToString();
        }


        private void button_reset_Click(object sender, EventArgs e)
        {
            trackBar_transmission.Value = 75;
            trackBar1.Value = 3;
            trackBar2.Value = 7;

            textBox_transmission_v.Text = ((double)trackBar_transmission.Value).ToString() + "%";
            textBox1.Text = ((int)trackBar1.Value * 2 - 1).ToString();
            textBox2.Text = ((int)trackBar2.Value).ToString();
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBox_transmission_v_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
