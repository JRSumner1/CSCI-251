using System;
using System.IO;
using SkiaSharp;

class Program
{
    // File paths for keystream and LFSR state
    private const string KeystreamFile = "keystream.txt";
    private const string LfsrStateFile = "lfsr_state.txt";

    // Entry point of the application
    static void Main(string[] args)
    {
        // If no arguments are provided or the first argument is "--help", display the help message
        if (args.Length == 0 || args[0].ToLower() == "--help")
        {
            ShowHelp();
            return;
        }

        // Convert the first argument to lowercase to handle case-insensitive commands
        string option = args[0].ToLower();

        // Switch based on the provided option to execute the corresponding functionality
        switch (option)
        {
            case "cipher":
                // Requires exactly 3 arguments: option, seed, and tap
                if (args.Length != 3)
                {
                    ShowHelp();
                    return;
                }
                CipherOption(args[1], args[2]);
                break;
            case "generatekeystream":
                // Requires exactly 4 arguments: option, seed, tap, and steps
                if (args.Length != 4)
                {
                    ShowHelp();
                    return;
                }
                GenerateKeystreamOption(args[1], args[2], args[3]);
                break;
            case "encrypt":
                // Requires exactly 2 arguments: option and plaintext in bits
                if (args.Length != 2)
                {
                    ShowHelp();
                    return;
                }
                EncryptOption(args[1]);
                break;
            case "decrypt":
                // Requires exactly 2 arguments: option and ciphertext in bits
                if (args.Length != 2)
                {
                    ShowHelp();
                    return;
                }
                DecryptOption(args[1]);
                break;
            case "multiplebits":
                // Requires exactly 5 arguments: option, seed, tap, steps, and iteration
                if (args.Length != 5)
                {
                    ShowHelp();
                    return;
                }
                MultipleBitsOption(args[1], args[2], args[3], args[4]);
                break;
            case "encryptimage":
            case "decryptimage":
                // Requires exactly 4 arguments: option, imagefile, seed, and tap
                if (args.Length != 4)
                {
                    ShowHelp();
                    return;
                }
                bool isEncryption = option == "encryptimage";
                EncryptDecryptImageOption(args[1], args[2], args[3], isEncryption);
                break;
            default:
                // If an unknown option is provided, display the help message
                ShowHelp();
                break;
        }
    }

    // Displays usage instructions and available options to the user
    static void ShowHelp()
    {
        Console.WriteLine("Usage: dotnet run <option> <other arguments>");
        Console.WriteLine(" - Cipher <seed> <tap>");
        Console.WriteLine(" - GenerateKeystream <seed> <tap> <steps>");
        Console.WriteLine(" - Encrypt <plaintext_bits>");
        Console.WriteLine(" - Decrypt <ciphertext_bits>");
        Console.WriteLine(" - MultipleBits <seed> <tap> <steps> <iterations>");
        Console.WriteLine(" - EncryptImage <imagefile> <seed> <tap>");
        Console.WriteLine(" - DecryptImage <imagefile> <seed> <tap>");
    }

    // Handles the 'Cipher' option: performs one step of the LFSR and displays the result
    static void CipherOption(string seed, string tapStr)
    {
        // Attempt to parse the tap position from string to integer
        if (!int.TryParse(tapStr, out int tap))
        {
            Console.WriteLine("Invalid tap position. It must be an integer.");
            return;
        }

        // Validate that the seed is a binary string
        if (!IsBinaryString(seed))
        {
            Console.WriteLine("Seed must be a binary string (containing only '0' and '1').");
            return;
        }

        // Initialize the LFSR with the provided seed and tap position
        LFSR lfsr = new LFSR(seed, tap);
        Console.WriteLine($"{seed} - seed");

        // Perform one step of the LFSR and retrieve the output bit
        int bit = lfsr.Step();
        Console.WriteLine($"{lfsr.GetSeed()}     {bit}");
    }

    // Handles the 'GenerateKeystream' option: generates a keystream of specified steps
    static void GenerateKeystreamOption(string seed, string tapStr, string stepsStr)
    {
        // Attempt to parse tap position and number of steps from strings to integers
        if (!int.TryParse(tapStr, out int tap) || !int.TryParse(stepsStr, out int steps))
        {
            Console.WriteLine("Invalid tap position or steps. Both must be integers.");
            return;
        }

        // Validate that the seed is a binary string
        if (!IsBinaryString(seed))
        {
            Console.WriteLine("Seed must be a binary string (containing only '0' and '1').");
            return;
        }

        // Initialize the LFSR with the provided seed and tap position
        LFSR lfsr = new LFSR(seed, tap);
        Console.WriteLine($"{seed} – seed");
        string keystream = "";

        // Generate the keystream by performing the specified number of steps
        for (int i = 0; i < steps; i++)
        {
            int bit = lfsr.Step();
            Console.WriteLine($"{lfsr.GetSeed()}   {bit}");
            keystream += bit.ToString();
        }

        Console.WriteLine($"The Keystream: {keystream}");

        // Save the generated keystream to a text file
        File.WriteAllText(KeystreamFile, keystream);
        Console.WriteLine("Keystream saved to keystream.txt");

        // Save the current LFSR state to the state file
        WriteSeedToStateFile(lfsr.GetSeed());
    }

    // Handles the 'Encrypt' option: encrypts plaintext using the generated keystream
    static void EncryptOption(string plaintext)
    {
        // Check if the keystream file exists
        if (!File.Exists(KeystreamFile))
        {
            Console.WriteLine("Keystream file not found. Please generate keystream first.");
            return;
        }

        // Validate that the plaintext is a binary string
        if (!IsBinaryString(plaintext))
        {
            Console.WriteLine("Invalid plaintext. It must be a binary string (containing only '0' and '1').");
            return;
        }

        // Read the keystream from the file and trim any whitespace
        string keystream = File.ReadAllText(KeystreamFile).Trim();

        // Read the current LFSR state from the state file
        if (!File.Exists(LfsrStateFile))
        {
            Console.WriteLine("LFSR state file not found. Please generate keystream first.");
            return;
        }

        string currentSeed = ReadSeedFromStateFile();
        if (currentSeed == null)
        {
            Console.WriteLine("Failed to read LFSR state. Please generate keystream first.");
            return;
        }

        // Initialize the LFSR with the current seed
        LFSR lfsr = new LFSR(currentSeed, 1); // Tap position is irrelevant here as it's already in the state

        // Calculate the required keystream length
        int requiredLength = plaintext.Length;
        int currentKeystreamLength = keystream.Length;

        if (currentKeystreamLength < requiredLength)
        {
            int additionalBitsNeeded = requiredLength - currentKeystreamLength;
            string additionalKeystream = "";

            // Generate additional keystream bits
            for (int i = 0; i < additionalBitsNeeded; i++)
            {
                int bit = lfsr.Step();
                additionalKeystream += bit.ToString();
            }

            // Append the additional keystream to the keystream file
            File.AppendAllText(KeystreamFile, additionalKeystream);
            Console.WriteLine($"Appended {additionalBitsNeeded} additional bits to the keystream.");

            // Update the keystream variable
            keystream += additionalKeystream;

            // Save the new LFSR state
            WriteSeedToStateFile(lfsr.GetSeed());
        }

        // Extract the relevant portion of the keystream
        string relevantKeystream = keystream.Substring(0, requiredLength);

        // Perform XOR between plaintext and keystream to generate ciphertext
        string ciphertext = XorStrings(plaintext, relevantKeystream);
        Console.WriteLine($"The ciphertext is: {ciphertext}");
    }

    // Handles the 'Decrypt' option: decrypts ciphertext using the generated keystream
    static void DecryptOption(string ciphertext)
    {
        // Check if the keystream file exists
        if (!File.Exists(KeystreamFile))
        {
            Console.WriteLine("Keystream file not found. Please generate keystream first.");
            return;
        }

        // Validate that the ciphertext is a binary string
        if (!IsBinaryString(ciphertext))
        {
            Console.WriteLine("Invalid ciphertext. It must be a binary string (containing only '0' and '1').");
            return;
        }

        // Read the keystream from the file and trim any whitespace
        string keystream = File.ReadAllText(KeystreamFile).Trim();

        // Read the current LFSR state from the state file
        if (!File.Exists(LfsrStateFile))
        {
            Console.WriteLine("LFSR state file not found. Please generate keystream first.");
            return;
        }

        string currentSeed = ReadSeedFromStateFile();
        if (currentSeed == null)
        {
            Console.WriteLine("Failed to read LFSR state. Please generate keystream first.");
            return;
        }

        // Initialize the LFSR with the current seed
        LFSR lfsr = new LFSR(currentSeed, 1); // Tap position is irrelevant here as it's already in the state

        // Calculate the required keystream length
        int requiredLength = ciphertext.Length;
        int currentKeystreamLength = keystream.Length;

        if (currentKeystreamLength < requiredLength)
        {
            int additionalBitsNeeded = requiredLength - currentKeystreamLength;
            string additionalKeystream = "";

            // Generate additional keystream bits
            for (int i = 0; i < additionalBitsNeeded; i++)
            {
                int bit = lfsr.Step();
                additionalKeystream += bit.ToString();
            }

            // Append the additional keystream to the keystream file
            File.AppendAllText(KeystreamFile, additionalKeystream);
            Console.WriteLine($"Appended {additionalBitsNeeded} additional bits to the keystream.");

            // Update the keystream variable
            keystream += additionalKeystream;

            // Save the new LFSR state
            WriteSeedToStateFile(lfsr.GetSeed());
        }

        // Extract the relevant portion of the keystream
        string relevantKeystream = keystream.Substring(0, requiredLength);

        // Perform XOR between ciphertext and keystream to retrieve plaintext
        string plaintext = XorStrings(ciphertext, relevantKeystream);
        Console.WriteLine($"The plaintext is: {plaintext}");
    }

    // Handles the 'MultipleBits' option: generates multiple bits from the LFSR
    static void MultipleBitsOption(string seed, string tapStr, string stepsStr, string iterationStr)
    {
        // Attempt to parse tap position, steps, and iterations from strings to integers
        if (!int.TryParse(tapStr, out int tap) || !int.TryParse(stepsStr, out int steps) || !int.TryParse(iterationStr, out int iterations))
        {
            Console.WriteLine("Invalid tap position, steps, or iterations. All must be integers.");
            return;
        }

        // Validate that the seed is a binary string
        if (!IsBinaryString(seed))
        {
            Console.WriteLine("Seed must be a binary string (containing only '0' and '1').");
            return;
        }

        // Initialize the LFSR with the provided seed and tap position
        LFSR lfsr = new LFSR(seed, tap);
        Console.WriteLine($"{seed} - seed");

        // Perform the specified number of iterations
        for (int i = 0; i < iterations; i++)
        {
            int variable = 0;

            // In each iteration, perform the specified number of steps to generate a variable
            for (int j = 0; j < steps; j++)
            {
                variable = (variable << 1) | lfsr.Step();
            }

            Console.WriteLine($"{lfsr.GetSeed()}     {variable}");
        }
    }

    // Handles both 'EncryptImage' and 'DecryptImage' options using a unified method
    static void EncryptDecryptImageOption(string imageFile, string seed, string tapStr, bool isEncryption)
    {
        // Attempt to parse the tap position from string to integer
        if (!int.TryParse(tapStr, out int tap))
        {
            Console.WriteLine("Invalid tap position. It must be an integer.");
            return;
        }

        // Check if the image file exists
        if (!File.Exists(imageFile))
        {
            Console.WriteLine("Image file not found.");
            return;
        }

        // Verify that the provided file is a valid image
        if (!IsImageFile(imageFile))
        {
            Console.WriteLine("The file provided is not a valid image.");
            return;
        }

        // Open and decode the image using SkiaSharp
        using var input = File.OpenRead(imageFile);
        using var bitmap = SKBitmap.Decode(input);

        // Check if the image was successfully decoded
        if (bitmap == null)
        {
            Console.WriteLine("Failed to decode the image. The file may not be a valid image.");
            return;
        }

        // Initialize the LFSR with the provided seed and tap position
        LFSR lfsr = new LFSR(seed, tap);

        // Iterate over each pixel in the image to encrypt/decrypt it
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor color = bitmap.GetPixel(x, y);

                // XOR each color channel with a randomly generated byte from the LFSR
                byte red = (byte)(color.Red ^ GenerateRandomByte(lfsr));
                byte green = (byte)(color.Green ^ GenerateRandomByte(lfsr));
                byte blue = (byte)(color.Blue ^ GenerateRandomByte(lfsr));

                // Create a new color with the encrypted/decrypted channels and the original alpha
                SKColor newColor = new SKColor(red, green, blue, color.Alpha);
                bitmap.SetPixel(x, y, newColor);
            }
        }

        // Define the output file name based on the operation
        string outputFileName = isEncryption
            ? $"{Path.GetFileNameWithoutExtension(imageFile)}ENCRYPTED{Path.GetExtension(imageFile)}"
            : GenerateDecryptedFileName(imageFile);

        using var output = File.OpenWrite(outputFileName);

        // Encode and save the encrypted/decrypted image in PNG format
        bitmap.Encode(output, SKEncodedImageFormat.Png, 100);
        string operation = isEncryption ? "Encrypted" : "Decrypted";
        Console.WriteLine($"{operation} image saved as {outputFileName}");
    }

    // Generates the decrypted image file name based on the encrypted file name
    static string GenerateDecryptedFileName(string imageFile)
    {
        string baseFileName = Path.GetFileNameWithoutExtension(imageFile);

        if (baseFileName.EndsWith("ENCRYPTED", StringComparison.OrdinalIgnoreCase))
        {
            baseFileName = baseFileName.Substring(0, baseFileName.Length - "ENCRYPTED".Length);
        }

        return $"{baseFileName}DECRYPTED{Path.GetExtension(imageFile)}";
    }

    // Generates a random byte by performing 8 LFSR steps
    static byte GenerateRandomByte(LFSR lfsr)
    {
        byte randomByte = 0;
        for (int i = 0; i < 8; i++)
        {
            int bit = lfsr.Step();
            randomByte = (byte)((randomByte << 1) | bit);
        }
        return randomByte;
    }

    // Checks if the provided file is a valid image by attempting to decode it
    static bool IsImageFile(string filePath)
    {
        try
        {
            using var input = File.OpenRead(filePath);
            using var bitmap = SKBitmap.Decode(input);
            return bitmap != null;
        }
        catch
        {
            // If an exception occurs, the file is not a valid image
            return false;
        }
    }

    // Performs a bitwise XOR between two binary strings and returns the result
    static string XorStrings(string s1, string s2)
    {
        // Determine the minimum length of the two strings to prevent out-of-range errors
        int length = Math.Min(s1.Length, s2.Length);
        char[] result = new char[length];

        // Iterate through each character and perform XOR operation
        for (int i = 0; i < length; i++)
        {
            result[i] = s1[i] == s2[i] ? '0' : '1';
        }

        return new string(result);
    }

    // Validates if a string consists solely of '0's and '1's
    static bool IsBinaryString(string input)
    {
        foreach (char c in input)
        {
            if (c != '0' && c != '1')
                return false;
        }
        return true;
    }

    // Reads the current seed from the LFSR state file
    static string ReadSeedFromStateFile()
    {
        try
        {
            return File.ReadAllText(LfsrStateFile).Trim();
        }
        catch
        {
            return null;
        }
    }

    // Writes the current seed to the LFSR state file
    static void WriteSeedToStateFile(string seed)
    {
        try
        {
            File.WriteAllText(LfsrStateFile, seed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to LFSR state file: {ex.Message}");
        }
    }

    // Inner class representing a Linear Feedback Shift Register (LFSR)
    public class LFSR
    {
        private string seed;          // Current state of the LFSR
        private int tapPosition;      // Tap position for feedback (1-based from the right)

        // Constructor to initialize the LFSR with a seed and tap position
        public LFSR(string seed, int tapPosition)
        {
            if (!IsBinaryString(seed))
            {
                throw new ArgumentException("Seed must be a binary string containing only '0' and '1'.", nameof(seed));
            }

            if (tapPosition < 1 || tapPosition > seed.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(tapPosition), "Tap position must be between 1 and the length of the seed.");
            }

            this.seed = seed;
            this.tapPosition = tapPosition;
        }

        // Performs one step of the LFSR and returns the output bit
        public int Step()
        {
            // Calculate the index of the tap bit (0-based index) from the right
            int tapBitIndex = seed.Length - tapPosition;

            // Extract the tap bit and the leftmost bit from the current seed
            int tapBit = seed[tapBitIndex] - '0';
            int leftmostBit = seed[0] - '0';

            // Compute the new bit as the XOR of the leftmost bit and the tap bit
            int newBit = leftmostBit ^ tapBit;

            // Update the seed by shifting left and appending the new bit
            seed = seed.Substring(1) + newBit.ToString();

            return newBit;
        }

        // Returns the current seed of the LFSR
        public string GetSeed()
        {
            return seed;
        }

        // Sets a new seed for the LFSR (not used in current implementation)
        public void SetSeed(string newSeed)
        {
            if (!IsBinaryString(newSeed))
            {
                throw new ArgumentException("New seed must be a binary string containing only '0' and '1'.", nameof(newSeed));
            }

            if (newSeed.Length != seed.Length)
            {
                throw new ArgumentException("New seed must be the same length as the current seed.", nameof(newSeed));
            }

            seed = newSeed;
        }

        // Validates if a string consists solely of '0's and '1's (static method)
        private static bool IsBinaryString(string input)
        {
            foreach (char c in input)
            {
                if (c != '0' && c != '1')
                    return false;
            }
            return true;
        }
    }
}
