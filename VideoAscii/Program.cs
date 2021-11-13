using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text;
using Emgu.CV;


namespace VideoAscii
{
    class Program
    {
        public static string pixels = " .-+*#";
        private static int lastTick;
        private static int lastFrameRate = 0;
        private static int frameRate = 0;
        
        static void Main(string[] args)
        {
            Console.Title = "Video to ASCII APP";
            Console.WriteLine("Video to ascii App!");
            Console.WriteLine("*********************\n");
            Console.WriteLine("\n Make sure you have your video ready on Input as video.mp4");
            Console.WriteLine("\nPress any key to continue . . .");
            Console.ReadKey();

            Console.Clear();

           //LoadVideo();
            bmpText(@"C:\\Users\\david\\source\\repos\\VideoAscii\\VideoAscii\\Text\\text.txt");

            Console.WriteLine("Thank you for using video to ASCII App!");
        }

        //Reads each pixel of the frame of the video and assigns an ascii character into that index of a string builder.
        //once a frame is ready it is printed and the next one starts building
        public static void bmpText(String savelocation)
        {
            SoundPlayer sp;


            StringBuilder sb = new StringBuilder();
            Console.Clear();
            Console.WriteLine("Preparing video . . .");
            System.IO.DirectoryInfo di = new DirectoryInfo(@"C:\\Users\\david\\source\\repos\\VideoAscii\\VideoAscii\\Output");
            int i = 0;
            if (OperatingSystem.IsWindows())
            {
                Assembly assembly;

                assembly = Assembly.GetExecutingAssembly();


                sp = new SoundPlayer(assembly.GetManifestResourceStream("C:\\Users\\david\\source\\repos\\VideoAscii\\VideoAscii\\Input\\video.wav"));
                sp.SoundLocation = "C:\\Users\\david\\source\\repos\\VideoAscii\\VideoAscii\\Input\\video.wav";
                sp.Load();

                sp.Play();

            }
            foreach (FileInfo file in di.GetFiles())
            {

                Console.Title = "Video to ASCII APP - PLAYING     frame: " + i + "   framerate: " + CalculateFrameRate() + "fps";
                var filename = Path.Combine(@"C:\\Users\\david\\source\\repos\\VideoAscii\\VideoAscii\\Output", $"{i}.bmp");
                var img = new Bitmap(filename);
                
                    //Console.WriteLine("Printing . . .");
                    
                
                for (int y = 0; y < img.Height; y++)
                    {
                        for (int z = 0; z < img.Width; z++)
                        {
                            var color = img.GetPixel(z, y);
                            var bright = Brightness(color);
                            double idx = bright / 255 * (pixels.Length - 1);
                            char pxl = pixels[(int)Math.Round(idx)];

                            sb.Append(pxl);
                            sb.Append(pxl);
                               
                        }
                        sb.Append("\n");
                    }

                //affects performance?????????
                //Removing it makes text jitter at the beginning
                    Console.SetCursorPosition(Console.WindowLeft, 0);
                    Console.Write(sb.ToString());
               //Console.WriteLine(CalculateFrameRate());
                

                    System.Threading.Thread.Sleep(17);
                //Console.Clear();
                sb.Clear();
                
                    i++;
            }
            

        }

        
        //Returns the brightness of the pixel on the frame of the video to be converted into an ascii char
        public static double Brightness(Color c)
        {
            return Math.Sqrt(
                c.R * c.R * .241 +
                c.G * c.G * .691 +
                c.B * c.B * .068
                
                );
        }

        //Clears the files and directories on the folder Output
        //load video file from Input (video.mp4) and convert each frame into a bmp image to be placed on Output
        public static void LoadVideo()
        {
            Console.WriteLine("Deleting files on Output . . .");
            Console.Title = "Video to ASCII APP - LOADING";
            System.IO.DirectoryInfo di = new DirectoryInfo(@"C:\\Users\\david\\source\\repos\\VideoAscii\\VideoAscii\\Output");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
            Console.Clear();

            Console.WriteLine("Loading video . . .");
            try
            {
                using (var video = new VideoCapture("C:\\Users\\david\\source\\repos\\VideoAscii\\VideoAscii\\Input\\video.mp4"))
                using (var img = new Mat())
                {
                    int i = 0;
                    while (video.Grab())
                    {

                        video.Retrieve(img);
                        var filename = Path.Combine(@"C:\\Users\\david\\source\\repos\\VideoAscii\\VideoAscii\\Output", $"{i}.bmp");
                        CvInvoke.Imwrite(filename, img);
                        i++;
                    }
                }
            }
            catch (Exception)
            {
                Console.Clear();
                Console.WriteLine("An error ocurred");
                Console.WriteLine("Video not found. \n Make sure the video is named like video.mp4 and place it in the Input folder");
                throw;
            }
            
        }

        public static int CalculateFrameRate()
        {
            if (System.Environment.TickCount - lastTick >= 1000)
            {
                lastFrameRate = frameRate;
                frameRate = 0;
                lastTick = System.Environment.TickCount;
            }
            frameRate++;
            
            
            return lastFrameRate;
        }


    }
}

