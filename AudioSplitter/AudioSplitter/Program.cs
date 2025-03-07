using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    //Notas 
    //Pasos para utilizar esto: 
    //1. Tener python instalado 
    //2. En la terminal de este proyecto instalar pytorch -> pip install torch torchvision torchaudio
    //3. Instalar ffmpeg -> https://ffmpeg.org/download.html
    //4. En la terminal de este proyecto instalar demucs -> pip install demucs
    //5. Listo :) Tomen en cuenta que algunas canciones pueden durar hasta 5 min en extraerse, una cancion de 2.48s puede durar aprox 90s 

    //Nota: Hice este código en menos de una hora así que no pienso optimizarlo XD
    //El proyecto acepta tanto video como audio
    static void Main()
    {
        List<string> files = SeparateChannels("Nombre de la cancion o el video.mp4"); //El archivo tiene que estar dentro de este proyecto
        
        MergeTracks(files[0], files[1], files[2], files[4]);
    }

    public static string ExtractAudio(string fileName, string outputDir, string inputFile)
    {
        string audioFile = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(fileName)}.wav");

        ProcessStartInfo ffmpegPsi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{inputFile}\" -vn -acodec pcm_s16le -ar 44100 -ac 2 \"{audioFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process ffmpegProcess = new Process { StartInfo = ffmpegPsi })
        {
            ffmpegProcess.Start();
            ffmpegProcess.WaitForExit();
        }
        return audioFile;
    }

    public static List<string> SeparateChannels(string fileName)
    {
        // Obtiene la ruta a la raíz del proyecto (tres niveles por encima de 'bin')
        string basePath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
        // Ruta relativa del archivo dentro del proyecto
        string inputFile = Path.Combine(basePath, fileName);
        // Ruta del directorio de salida dentro del proyecto
        string outputDir = Path.Combine(basePath, "output");
        //Mapeo de la ruta final aunque tal vez no exista aun
        string folderDemucsPath = Path.Combine(outputDir, "htdemucs", Regex.Replace(fileName, @"\..*", ""));

        // Verifica si el directorio de salida existe, si no lo crea
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        inputFile = fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
        ? ExtractAudio(fileName, outputDir, inputFile)
        : inputFile;

        // Configuración del proceso para ejecutar Demucs
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"-m demucs \"{inputFile}\" -o \"{outputDir}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = psi };
        process.Start();
        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Console.WriteLine(result);

        string bass = Path.Combine(folderDemucsPath, "bass.wav");
        string drums = Path.Combine(folderDemucsPath, "drums.wav");
        string other = Path.Combine(folderDemucsPath, "other.wav");
        string vocals = Path.Combine(folderDemucsPath, "vocals.wav");
        string mergedOutputFile = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(inputFile)}_merged.wav");

        return new List<string>()
        {
            bass,
            drums,
            other,
            vocals,
            mergedOutputFile
        };
    }

    static void MergeTracks(string bassFile, string drumsFile, string otherFile, string outputFile)
    {
        if (File.Exists(outputFile))
        {
            Console.WriteLine($"El archivo combinado ya existe: {outputFile}");
            Console.Write("¿Deseas regenerarlo? (s/n): ");
            string respuesta = Console.ReadLine();
            if (respuesta.Trim().ToLower() != "s")
            {
                Console.WriteLine("Saliendo del programa sin regenerar el archivo.");
                return;
            }
        }

        if (File.Exists(bassFile) && File.Exists(drumsFile) && File.Exists(otherFile))
        {
            Console.WriteLine("Combinando bass, drums y other en un solo archivo...");

            ProcessStartInfo ffmpegPsi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{bassFile}\" -i \"{drumsFile}\" -i \"{otherFile}\" -filter_complex \"[0:a][1:a][2:a]amix=inputs=3:duration=longest\" \"{outputFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process ffmpegProcess = new Process { StartInfo = ffmpegPsi };
            ffmpegProcess.Start();
            string ffmpegOutput = ffmpegProcess.StandardOutput.ReadToEnd();
            string ffmpegError = ffmpegProcess.StandardError.ReadToEnd();
            ffmpegProcess.WaitForExit();

            Console.WriteLine("FFmpeg Salida estándar:");
            Console.WriteLine(ffmpegOutput);
            if (!string.IsNullOrEmpty(ffmpegError))
            {
                Console.WriteLine("FFmpeg Errores:");
                Console.WriteLine(ffmpegError);
            }

            Console.WriteLine($"Archivo combinado creado: {outputFile}");
        }
        else
        {
            Console.WriteLine("Error: No se encontraron todas las pistas necesarias para la mezcla.");
        }

    }
}
