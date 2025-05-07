using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text;
using Emgu.CV;
using System.Windows.Forms;
using Xabe.FFmpeg;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Logging;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using NAudio.Wave;

namespace VideoAscii
{

    
    class Program
    {
        public static string pixels = " .-+*#";
        private static int lastTick;
        private static int lastFrameRate = 0;
        private static int frameRate = 0;
       // OpenFileDialog od = new OpenFileDialog();
        private static string filepath;
        private static double videoFps = 30.0; // valor por defecto si no se puede leer
        private static string wavpath;
        private static double frameDelay = 0;
        private static int videofps = 0;

        [STAThread]
        static void Main(string[] args)
        {

          //  Process.Start("cmd.exe", "/K mode con: cols=1000 lines=1000");


            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Input"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Input");
            }

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Output"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Output");
            }

            Console.Title = "Video to ASCII APP";
            Console.WriteLine("Video to ascii App!");
            Console.WriteLine("*********************\n");
            Console.WriteLine("\n Make sure you have your video ready on Input as video.mp4");
            Console.WriteLine("\n This windows will go fullscreen once you continue");
            Console.WriteLine("\nPress any key to continue . . .");

            Console.ReadKey();
#pragma warning disable CA1416 // Validate platform compatibility
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
            Console.SetBufferSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
#pragma warning restore CA1416 // Validate platform compatibility

            Console.Clear();

           LoadVideo();
            ConvertAudio().GetAwaiter().GetResult();
            bmpText();

            Console.WriteLine("Thank you for using video to ASCII App!");
        }

        //Reads each pixel of the frame of the video and assigns an ascii character into that index of a string builder.
        //once a frame is ready it is printed and the next one starts building

        public static async Task ConvertAudio()
        {
            Console.Clear();
            Console.WriteLine("Converting audio...");
            wavpath = @".\Output\video.wav";
           // wavpath = filepath.Replace(".mp4", ".wav");
            var snippet = await FFmpeg.Conversions.FromSnippet.ExtractAudio(filepath, wavpath);
            IConversionResult result = await snippet.Start();
        }

        public static void bmpText()
        {
            
            SoundPlayer sp;
            StringBuilder sb = new StringBuilder();
            Console.Clear();
            Console.WriteLine("Preparing video . . .");

            string[] bmpFiles = Directory.GetFiles(@".\Output", "*.bmp").OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f))).ToArray();
            int i = 0;

            // Tamaño fijo para el render en ASCII
            const int asciiWidth = 80;
            const int asciiHeight = 40;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    sp = new SoundPlayer(wavpath);
                    sp.Load();
                    sp.Play();
                }
                catch (Exception)
                {
                    Console.WriteLine("Error: no se pudo cargar el audio. Asegúrate de que video.wav esté presente en la carpeta Output.");
                    Console.ReadLine();
                    return;
                }
            }
            Stopwatch totalStopwatch = new Stopwatch();
            Stopwatch frameStopwatch = new Stopwatch();
            double videoDuration = bmpFiles.Length / videoFps; // duración del video en segundos
            double audioDuration = new AudioFileReader(wavpath).TotalTime.TotalSeconds; // duración del audio en segundos

            if (Math.Abs(videoDuration - audioDuration) > 0.1)
            {
                Console.WriteLine("Warning: Video and audio durations are not synchronized.");
            }

            Stopwatch syncStopwatch = new Stopwatch();
            syncStopwatch.Start();

            foreach (string filename in bmpFiles)
            {
                Console.Title = $"Video to ASCII APP - PLAYING     frame: {i}   framerate: {CalculateFrameRate()}fps";

                Bitmap original = null;
                Bitmap resized = null; totalStopwatch.Start(); // Comienza a medir el tiempo total

                try
                {
                    frameStopwatch.Start(); // Comienza a medir el tiempo para procesar el frame
                    original = new Bitmap(filename);
                    resized = new Bitmap(original, new Size(asciiWidth, asciiHeight));
                    frameStopwatch.Stop(); // Detiene el temporizador de procesamiento del frame
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al procesar {filename}: {ex.Message}");
                    continue;
                }

                for (int y = 0; y < resized.Height; y++)
                {
                    for (int x = 0; x < resized.Width; x++)
                    {
                        var color = resized.GetPixel(x, y);
                        var bright = Brightness(color);
                        double idx = bright / 255 * (pixels.Length - 1);
                        char pxl = pixels[(int)Math.Round(idx)];

                        sb.Append(pxl);
                        sb.Append(pxl); // Para compensar aspecto ancho de las letras
                    }
                    sb.Append('\n');
                }

                Console.SetCursorPosition(0, 0);
                Console.Write(sb.ToString());

                sb.Clear();
                resized.Dispose();
                original.Dispose();

                Stopwatch sw = Stopwatch.StartNew();

                // Renderizado del frame
                Console.SetCursorPosition(Console.WindowLeft, 0);
                Console.Write(sb.ToString());
                sb.Clear();
                i++;

                // Calcular cuánto tiempo queda por esperar
                sw.Stop();
                //frameDelay = 33;
                int elapsed = (int)sw.ElapsedMilliseconds;
                double wait = Math.Max(0, frameDelay - elapsed); // Ajuste aquí para evitar wait negativo
                Console.Title = $"Frame: {i} | Delay: {frameDelay}ms | Processing: {elapsed}ms | Wait: {wait}ms | FPS: {videofps}ms" ;
               // Console.Title = $"Frame {i} - Total time: {totalStopwatch.ElapsedMilliseconds}ms | Frame processing time: {frameStopwatch.ElapsedMilliseconds}ms";

                double audioProgress = syncStopwatch.Elapsed.TotalSeconds; // Tiempo en segundos del audio
                double frameProgress = i / videoFps; // Tiempo en segundos del video

                if (Math.Abs(audioProgress - frameProgress) > 0.05)
                {
                    int delayCorrection = (int)((frameProgress - audioProgress) * 1000); // corrección en milisegundos
                    wait += delayCorrection; // Ajustar la espera para corregir la desincronización
                }

                if (wait > 0)
                {
                    var start = DateTime.Now;
                    while ((DateTime.Now - start).TotalMilliseconds < wait)
                    {
                        // Mantener el CPU ocupado pero de manera controlada (sin usar Thread.Sleep)
                        // Podrías dejar esta parte vacía o agregar alguna operación ligera si es necesario
                    }
                }
                // Resetear el temporizador total y frame para el siguiente frame
                totalStopwatch.Reset();
                frameStopwatch.Reset();

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
            System.IO.DirectoryInfo di = new DirectoryInfo(@".\Output");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
            Console.Clear();

            Console.WriteLine("Please choose a video file to convert");
           
            try
            {
                OpenFileDialog od1 = new OpenFileDialog();
                od1.Title = "Choose a video";
                od1.Filter = "Video file|*.mp4";
                od1.ShowDialog();
                filepath = od1.FileName;
                Console.Clear();
                // Console.WriteLine("Loading video . . .");
                String prepare = new string('-', 50);
                Console.Write($"Processing video frames: [{prepare}] 0.0%");

                using (var video = new VideoCapture(filepath))
                {
                    int width = (int)video.Get(Emgu.CV.CvEnum.CapProp.FrameWidth);
                    int height = (int)video.Get(Emgu.CV.CvEnum.CapProp.FrameHeight);

                    // Check if the video is widescreen (aspect ratio is greater than 4:3)
                    if ((double)width / height > 1.33) // Widescreen aspect ratio check
                    {
                        // Crop and resize the video to 480x360 using FFmpeg
                        string outputVideoPath = @".\Output\cropped_video.mp4";
                        string ffmpegArgs = $"-i \"{filepath}\" -vf \"fps=30, crop=iw:ih-60,scale=480:360\" -c:a copy \"{outputVideoPath}\"";

                        // Execute the FFmpeg command with the custom arguments
                        var conversion = FFmpeg.Conversions.New().AddParameter(ffmpegArgs);
                        conversion.Start().GetAwaiter().GetResult();

                        // Set filepath to the new cropped video
                        filepath = outputVideoPath;
                    }
                    else
                    {
                        // If the video is not widescreen, we can resize it directly to 480x360
                        string outputVideoPath = @".\Output\resized_video.mp4";
                        string ffmpegArgs = $"-i \"{filepath}\" -vf \"fps=30,scale=480:360\" -c:a copy \"{outputVideoPath}\"";

                        // Execute the FFmpeg command with the custom arguments
                        var conversion = FFmpeg.Conversions.New().AddParameter(ffmpegArgs);
                        conversion.Start().GetAwaiter().GetResult();

                        // Set filepath to the resized video
                        filepath = outputVideoPath;
                    }
                }

                using (var video = new VideoCapture(filepath.ToString()))
                using (var img = new Mat())
                {
                    double fps = video.Get(Emgu.CV.CvEnum.CapProp.Fps);
                    videofps = Convert.ToInt16(fps);
                    if (fps <= 0 || double.IsNaN(fps)) fps = 1; // valor por defecto
                    frameDelay = (int)(1000.0 / fps);
                    int totalFrames = (int)video.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                    int i = 0;
                    if (video.Grab())
                    {

                        Console.Clear();
                        while (video.Grab())
                        {

                            video.Retrieve(img);
                            var filename = Path.Combine(@".\Output", $"{i}.bmp");
                            CvInvoke.Imwrite(filename, img);
                            i++;
                            double percent = (i / (double)totalFrames) * 100.0;
                            int totalBlocks = 50;
                            int filledBlocks = (int)(percent / 100 * totalBlocks);
                            string bar = new string('#', filledBlocks).PadRight(totalBlocks, '-');
                            Console.CursorLeft = 0;
                            Console.Write($"Processing video frames: [{bar}] {percent:0.0}%");
                        }
                    }
                    else
                    {

                        Console.Clear();
                        Console.WriteLine("An error ocurred");
                        Console.WriteLine("Video not found. \n Make sure the video is named like video.mp4 and place it in the Input folder");
                        Console.ReadKey();
                        return;
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

