using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//EMGU
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;

namespace Defogging
{
    public static class Core
    {
        static double alpha = 0.05; //透射率
        static int Airlightp;   //之前的大气光值
        static int Airlight;    //当前大气光值
        static int FrameCount = 0;  //迭代次数
        static int ad;  //当前大气光值参考量
        //static double[] rgbM = new double[3] { 1.0, 1.0, 1.0 };//RGB三通道弱化系数
        static double transmissionWeight = 0.75;//通透率权重
        static int filterSize = 5;
        static int guidedFilterSize = 7;

        //计算中值滤波的暗通道
        private static Image<Gray, byte> GetMedianDarkChannel(Image<Bgr, byte> src, int patch)
        {
            Image<Gray, byte> rgbmin = new Image<Gray, byte>(src.Width, src.Height);

            for (int m = 0; m < src.Width; m++)
            {
                for (int n = 0; n < src.Height; n++)
                {
                    rgbmin.Data[n, m, 0] = Math.Min(Math.Min(src.Data[n, m, 0], src.Data[n, m, 1]), src.Data[n, m, 2]);
                }
            }
            rgbmin = rgbmin.SmoothMedian(patch);
            return rgbmin;
        }

        //输入图像为单通道,预估的透射率图 引导图像为单通道，原图像的灰度图 输出图像为单通道，导向滤波后的透射图
        private static Image<Gray, byte> GuidedFilter(Image<Gray, Byte> p, Image<Gray, Byte> I, int r, double e)
        {
            //int r,  r;
            //w = h = 2 * r + 1;

            Image<Gray, byte> mean_p = new Image<Gray, byte>(p.Width, p.Height);
            Image<Gray, byte> mean_I = new Image<Gray, byte>(I.Width, I.Height);

            Image<Gray, byte> II = new Image<Gray, byte>(I.Width, I.Height);
            Image<Gray, byte> Ip = new Image<Gray, byte>(I.Width, I.Height);

            Image<Gray, byte> corr_II = new Image<Gray, byte>(I.Width, I.Height);
            Image<Gray, byte> corr_Ip = new Image<Gray, byte>(I.Width, I.Height);

            Image<Gray, byte> var_II = new Image<Gray, byte>(I.Width, I.Height);
            Image<Gray, byte> cov_Ip = new Image<Gray, byte>(I.Width, I.Height);

            Image<Gray, byte> a = new Image<Gray, byte>(I.Width, I.Height);
            Image<Gray, byte> b = new Image<Gray, byte>(I.Width, I.Height);

            Image<Gray, byte> mean_a = new Image<Gray, byte>(I.Width, I.Height);
            Image<Gray, byte> mean_b = new Image<Gray, byte>(I.Width, I.Height);

            Image<Gray, byte> q = new Image<Gray, byte>(p.Width, p.Height);

            //利用 boxFilter 计算均值  原始均值 导向均值  自相关均值  互相关均值
            CvInvoke.BoxFilter(p, mean_p, DepthType.Cv8U, new Size(r,  r), new Point(-1, -1), true, BorderType.Reflect101);
            CvInvoke.BoxFilter(I, mean_I, DepthType.Cv8U, new Size(r,  r), new Point(-1, -1), true, BorderType.Reflect101);
 
            CvInvoke.Multiply(I, I, II);
            CvInvoke.Multiply(I, p, Ip);

            CvInvoke.BoxFilter(II, corr_II, DepthType.Cv8U, new Size(r,  r), new Point(-1, -1), true, BorderType.Reflect101);
            CvInvoke.BoxFilter(Ip, corr_Ip, DepthType.Cv8U, new Size(r,  r), new Point(-1, -1), true, BorderType.Reflect101);

            CvInvoke.Multiply(mean_I, mean_I, var_II);
            CvInvoke.Subtract(corr_II, var_II, var_II);

            CvInvoke.Multiply(mean_I, mean_p, cov_Ip);
            CvInvoke.Subtract(corr_Ip, cov_Ip, cov_Ip);

            CvInvoke.Divide(cov_Ip, var_II + e, a);
            CvInvoke.Multiply(a, mean_I, b);
            CvInvoke.Subtract(mean_p, b, b);

            CvInvoke.BoxFilter(a, mean_a, DepthType.Cv8U, new Size(r,  r), new Point(-1, -1), true, BorderType.Reflect101);
            CvInvoke.BoxFilter(b, mean_b, DepthType.Cv8U, new Size(r,  r), new Point(-1, -1), true, BorderType.Reflect101);

            CvInvoke.Multiply(mean_a, I, q);
            CvInvoke.Add(mean_b, q, q);

            return q;
        }

        //根据暗通道中的最亮的像素来估算大气光值（基于何凯明的理论）
        private static int EstimateA(Image<Gray, byte> DC)
        {
            DC.MinMax(out double[] minDC, out double[] maxDC, out Point[] minDC_Loc, out Point[] maxDC_Loc);
            return (int)maxDC[0];
        }


        //估算透光率
        private static Image<Gray, Byte> EstimateTransmission(Image<Gray, Byte> DCP, int ac)
        {
            //double w = 0.75;
            Image<Gray, Byte> transmission = new Image<Gray, byte>(DCP.Width, DCP.Height);

            for (int m = 0; m < DCP.Width; m++)
            {
                for (int n = 0; n < DCP.Height; n++)
                {
                    //transmission.Data[n, m, 0] = (byte)((1 - w * (double)DCP.Data[n, m, 0] / ac) * 255);
                    transmission.Data[n, m, 0] = (byte)((1 - transmissionWeight * (double)DCP.Data[n, m, 0] / ac) * 255);
                }
            }
            return transmission;
        }

        //调整透光率及滤波器大小
        public static void Adjust(double w, int p, int q)
        {
            transmissionWeight = w;
            filterSize = p;
            guidedFilterSize = q;
        }

        //去雾
        private static Image<Bgr, Byte> GetDehazed(Image<Bgr, Byte> source, Image<Gray, Byte> t, int al)
        {
            double tmin = 0.1;
            double tmax;

            Image<Bgr, Byte> dehazed = source.CopyBlank();
            
            for (int i = 0; i < source.Width; i++)
            {
                for (int j = 0; j < source.Height; j++)
                {
                    tmax = (double)t.Data[j, i, 0] / 255;
                    tmax = tmax < tmin ? tmin : tmax;
                    for (int k = 0; k < 3; k++)
                    {
                        var red = Math.Abs(((double)source.Data[j, i, 2] - al) / tmax + al);
                        var green = Math.Abs(((double)source.Data[j, i, 1] - al) / tmax + al);
                        var blue = Math.Abs(((double)source.Data[j, i, 0] - al) / tmax + al);
                        red = red > 255 ? 255 : red;
                        green = green > 255 ? 255 : green;
                        blue = blue > 255 ? 255 : blue;
                        dehazed.Data[j, i, 2] = (byte)(red);
                        dehazed.Data[j, i, 1] = (byte)(green);
                        dehazed.Data[j, i, 0] = (byte)(blue);

                    }
                }
            }
            return dehazed;
        }
        

        public static Bitmap[] Dehaze(Image<Bgr, Byte> src, int method)
        {
            Image<Gray, Byte> darkChannel;
            Image<Gray, Byte> T;
            Image<Bgr, Byte> fogfree = src.CopyBlank();
            Image<Gray, Byte> p = new Image<Gray, byte>(src.Width, src.Height);
            CvInvoke.CvtColor(src, p, ColorConversion.Bgr2Gray);//转换为灰度图

            //rgbM = rgb3v;
   
            darkChannel = GetMedianDarkChannel(src, filterSize);
            Airlight = EstimateA(darkChannel);
            T = EstimateTransmission(darkChannel, Airlight);

            if(method == 1)
            {
                T = GuidedFilter(T, p, guidedFilterSize, 0.001);
            }

            ad = FrameCount == 0 ? Airlight : (int)(alpha * (double)(Airlight) + (1 - alpha) * (double)(Airlightp));//平均化大气光值
            fogfree = GetDehazed(src, T, ad);
            Airlightp = ad;

            FrameCount++;
            //return fogfree;
            return new Bitmap[] { fogfree.ToBitmap(), darkChannel.ToBitmap(), T.ToBitmap() };
        }

    }
}
