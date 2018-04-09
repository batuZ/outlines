using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 自动调色
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.pictureBox1.Image = Image.FromFile(@"E:\test\DSC00661.JPG");
 
        }

        private void Form1_Load(object sender, EventArgs e)
        {


            FileStream fs = new FileStream(@"E:\test\DSC00661.JPG", FileMode.Open, FileAccess.Read);
            byte[] buffByte = new byte[fs.Length];
            fs.Read(buffByte, 0, (int)fs.Length);
            fs.Close();

            Bitmap pp = new Bitmap(@"E:\test\DSC00661.JPG");
          
            int w = pp.Width;
            int h = pp.Height;

            int[,] R = new int[w, h];
            int[,] G = new int[w, h];
            int[,] B = new int[w, h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = pp.GetPixel(x, y);
                    R[x, y] = c.R;
                    G[x, y] = c.G;
                    B[x, y] = c.B;
                }
            }

         
        }
    }

    class Programa
    {
        static string src_path;
        static long small_size = 0;

        private static ImageCodecInfo GetCodecInfo(string mimeType)
        {
            ImageCodecInfo[] CodecInfo = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo ici in CodecInfo)
            {
                if (ici.MimeType == mimeType)
                    return ici;
            }
            return null;
        }

        static void SaveImage(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);

            if (stream.Length == 0)
            {
                stream.Close();
                return;
            }

            byte[] file_data = new byte[stream.Length];
            stream.Read(file_data, 0, (int)stream.Length);
            Stream mem = new MemoryStream(file_data);

            long old_size = stream.Length;

            try
            {
                Image img = new Bitmap(mem);
                stream.Close();

                EncoderParameter p = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);
                EncoderParameters ps = new EncoderParameters(1);

                ps.Param[0] = p;

                img.Save(path, GetCodecInfo("image/jpeg"), ps);

                FileStream f = new FileStream(path, FileMode.Open, FileAccess.Read);
                long new_size = f.Length;
                f.Close();

                small_size += old_size - new_size;

            }
            catch (System.Exception ex)
            {

            }
            finally
            {
                stream.Close();
            }

        }

        static void ConvertOneImage(string path, bool is_save)
        {
            if (is_save)
            {
                string new_name = src_path + path.Substring(path.LastIndexOf('\\'));
                File.Copy(path, new_name, true);
            }

            SaveImage(path);
        }

        static void ShowSize(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open);
            Image img = Image.FromStream(stream, false, false);
            stream.Close();
            //Console.WriteLine("{0} {1}", img.Width, img.Height);
            if (img.Width == 0)
            {
                Console.WriteLine("Error");
            }
        }

        static void BatchJpeg()
        {
            string path = Application.ExecutablePath;
            path = path.Substring(0, path.LastIndexOf('\\'));

            src_path = path + "\\" + "src";
            //Console.WriteLine(src_path);

            Console.WriteLine("批量转化jpeg图片，保证其图片质量的前提下减少其存储大小");
            Console.WriteLine("若想保存原图片，其按y(原图将放在src文件夹下), 否则按任意键开始处理");

            ConsoleKeyInfo key = Console.ReadKey();
            bool is_save = false;

            if (key.KeyChar == 'y' || key.KeyChar == 'Y')
            {
                is_save = true;

                Directory.CreateDirectory("src");
            }

            string[] files = Directory.GetFiles(path, "*.jpg");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < files.Length; ++i)
            {
                ConvertOneImage(files[i], is_save);
                //string s = files[i].Substring(files[i].LastIndexOf('\\') + 1);
                //Console.WriteLine((i + 1).ToString() + "/" + files.Length.ToString() + " \t " + s);

                //ShowSize(files[i]);
            }

            sw.Stop();

            Console.WriteLine("*********已结束，按任意键结束********");
            double v = (small_size * 1.0 / (1024 * 1024));

            Console.WriteLine("共减少 " + v.ToString("0.00##") + "M 的存储空间");
            Console.WriteLine("耗时:" + sw.Elapsed.TotalSeconds.ToString("0.00##") + "秒");
            Console.ReadKey();
        }

        static void Main(string[] args)
        {
            BatchJpeg();
            //SaveImage("E:\\img - 副本.JPG");
            //string path = "E:\\cpp_app\\LiteTools\\JpegBatch\\bin\\Release\\img - 副本.JPG";
            //File.Delete(path);
        }
    }
    
}
