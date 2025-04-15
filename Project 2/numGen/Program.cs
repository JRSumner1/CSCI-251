using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NumGen
{
    class Program
    {
        // Prints command usage instructions to the console
        static void PrintHelp()
        {
            Console.WriteLine("dotnet run <bits> <option> <count>");
            Console.WriteLine("   - bits - the number of bits of the number to be generated, this must be a multiple of 8, and at least 32 bits");
            Console.WriteLine("   - option - 'odd' or 'prime' (the type of numbers to be generated)");
            Console.WriteLine("   - count - the count of numbers to generate, defaults to 1");
        }

        static void Main(string[] args)
        {
            // Validate input arguments and provide help if incorrect usage
            if (args.Length < 2 || args.Length > 3)
            {
                PrintHelp();
                return;
            }

            // Validate that the bit length is a multiple of 8 and at least 32
            if (!int.TryParse(args[0], out int bits) || bits < 32 || bits % 8 != 0)
            {
                PrintHelp();
                return;
            }

            // Validate that the proper option was entered
            string option = args[1].ToLower();
            if (option != "prime" && option != "odd")
            {
                PrintHelp();
                return;
            }
            
            // Validate that the proper count was entered
            int count = 1;
            if (args.Length == 3 && (!int.TryParse(args[2], out count) || count < 1))
            {
                PrintHelp();
                return;
            }

            // Print the bit length to the console
            Console.WriteLine($"BitLength: {bits} bits");

            // Start the stopwatch to measure the time taken for generation
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Decide which type of number to generate (prime or odd)
            if (option == "prime")
            {
                GeneratePrimes(bits, count);
            }
            else if (option == "odd")
            {
                GenerateOdds(bits, count);
            }

            // Stop the stopwatch and print the time taken
            stopwatch.Stop();
            Console.WriteLine($"Time to Generate: {stopwatch.Elapsed:hh\\:mm\\:ss\\.ffffff}");
        }

        // Generates the specified number of prime numbers with the given bit length
        static void GeneratePrimes(int bits, int count)
        {
            int found = 0; // Counter for the number of primes found
            object lockObj = new object(); // Lock object for thread synchronization

            // Use parallel processing to generate primes
            Parallel.For(0, int.MaxValue, (i, state) =>
            {
                // If the required number of primes have been found, stop the loop
                if (found >= count)
                {
                    state.Stop();
                    return;
                }

                // Generate a random big integer of the specified bit length
                BigInteger number = GenerateRandomBigInteger(bits);

                // Check if the number is probably prime using Miller-Rabin test
                if (number.IsProbablyPrime())
                {
                    lock (lockObj) // Ensure thread-safe access to shared variables
                    {
                        // Double-check if the required number of primes have been found
                        if (found >= count)
                        {
                            state.Stop();
                            return;
                        }

                        found++; // Increment the count of primes found
                        Console.WriteLine($"{found}: {number}"); // Output the prime number

                        // If the required number of primes have been found, stop the loop
                        if (found >= count)
                        {
                            state.Stop();
                        }
                    }
                }
            });
        }

        // Generates the specified number of odd numbers with the given bit length and counts their factors
        static void GenerateOdds(int bits, int count)
        {
            int found = 0; // Counter for the number of odd numbers found
            object lockObj = new object(); // Lock object for thread synchronization

            // Use parallel processing to generate odd numbers
            Parallel.For(0, int.MaxValue, (i, state) =>
            {
                // If the required number of odd numbers have been found, stop the loop
                if (found >= count)
                {
                    state.Stop();
                    return;
                }

                // Generate a random big integer of the specified bit length
                BigInteger number = GenerateRandomBigInteger(bits);

                // Check if the number is odd
                if (number % 2 != 0)
                {
                    // Count the number of prime factors (including multiplicities)
                    int factors = CountFactors(number);

                    lock (lockObj) // Ensure thread-safe access to shared variables
                    {
                        // Double-check if the required number of odd numbers have been found
                        if (found >= count)
                        {
                            state.Stop();
                            return;
                        }

                        found++; // Increment the count of odd numbers found
                        Console.WriteLine($"{found}: {number}"); // Output the odd number
                        Console.WriteLine($"Number of factors: {factors}"); // Output the number of factors

                        // If the required number of odd numbers have been found, stop the loop
                        if (found >= count)
                        {
                            state.Stop();
                        }
                    }
                }
            });
        }

        // Generates a random BigInteger of the specified bit length
        static BigInteger GenerateRandomBigInteger(int bits)
        {
            byte[] bytes = new byte[bits / 8]; // Create a byte array of the required size
            RandomNumberGenerator.Fill(bytes); // Fill the byte array with cryptographically strong random bytes

            // Set the most significant bit to ensure the number has the desired bit length
            bytes[0] |= 0x80;

            // Ensure the number is positive by clearing the sign bit
            bytes[bytes.Length - 1] &= 0x7F;

            BigInteger number = new BigInteger(bytes); // Create a BigInteger from the byte array
            return BigInteger.Abs(number); // Return the absolute value (should be positive)
        }

        // Counts the total number of prime factors (including multiplicities) of the given number
        static int CountFactors(BigInteger number)
        {
            int count = 0; // Initialize the count of factors
            BigInteger sqrt = Sqrt(number); // Compute the integer square root of the number

            // Check for divisibility by 2 separately
            if (number % 2 == 0)
            {
                int factorCount = 0;
                while (number % 2 == 0)
                {
                    number /= 2;
                    factorCount++;
                }
                count += factorCount;
            }

            // Check for odd divisors from 3 up to the square root of the number
            for (BigInteger i = 3; i <= sqrt; i += 2)
            {
                if (number % i == 0)
                {
                    int factorCount = 0;
                    while (number % i == 0)
                    {
                        number /= i;
                        factorCount++;
                    }
                    count += factorCount;
                    sqrt = Sqrt(number); // Update the square root because the number has changed
                }
            }

            // If the remaining number is a prime number greater than 2, count it
            if (number > 2)
            {
                count++;
            }

            return count; // Return the total count of prime factors
        }

        // Computes the integer square root of a BigInteger using Newton's method
        static BigInteger Sqrt(BigInteger n)
        {
            if (n == 0) return 0;
            if (n > 0)
            {
                // Estimate the initial guess for the square root
                BigInteger bitLength = (BigInteger)Math.Ceiling(BigInteger.Log(n, 2));
                BigInteger root = BigInteger.One << (int)(bitLength >> 1);

                // Refine the guess using Newton-Raphson iteration
                while (!IsSqrt(n, root))
                {
                    root = (root + n / root) >> 1;
                }

                return root; // Return the computed integer square root
            }
            throw new ArithmeticException("NaN"); // Cannot compute square root of negative number
        }

        // Checks if 'root' is the integer square root of 'n'
        static bool IsSqrt(BigInteger n, BigInteger root)
        {
            BigInteger lowerBound = root * root;
            BigInteger upperBound = (root + 1) * (root + 1);

            return (n >= lowerBound && n < upperBound);
        }
    }

    // Extension methods for BigInteger
    public static class BigIntegerExtensions
    {
        // Performs the Miller-Rabin primality test to check if a number is probably prime
        public static bool IsProbablyPrime(this BigInteger value, int witnesses = 10)
        {
            if (value <= 1 || value == 4) return false; // 1 and 4 are not prime
            if (value <= 3) return true; // 2 and 3 are prime

            // Write n - 1 as 2^r * d
            BigInteger d = value - 1;
            int r = 0;

            while (d % 2 == 0)
            {
                d /= 2;
                r++;
            }

            // WitnessLoop: Perform the test for the specified number of witnesses
            for (int i = 0; i < witnesses; i++)
            {
                // Pick a random integer 'a' in [2, value - 2]
                BigInteger a = RandomBigInteger(2, value - 2);
                BigInteger x = BigInteger.ModPow(a, d, value);

                if (x == 1 || x == value - 1)
                    continue; // Possibly prime, continue to next witness

                bool continueOuter = false;

                // Repeat r - 1 times
                for (int j = 0; j < r - 1; j++)
                {
                    x = BigInteger.ModPow(x, 2, value);

                    if (x == 1)
                        return false; // Composite number

                    if (x == value - 1)
                    {
                        continueOuter = true; // Possibly prime, proceed to next witness
                        break;
                    }
                }

                if (continueOuter)
                    continue; // Continue with next witness

                return false; // Composite number
            }

            return true; // Probably prime
        }

        // Generates a random BigInteger in the range [min, max)
        private static BigInteger RandomBigInteger(BigInteger min, BigInteger max)
        {
            byte[] bytes = max.ToByteArray();
            BigInteger a;

            do
            {
                RandomNumberGenerator.Fill(bytes); // Fill bytes with random data
                bytes[bytes.Length - 1] &= 0x7F; // Ensure the number is positive
                a = new BigInteger(bytes);
            } while (a < min || a >= max); // Ensure the random number is within the desired range

            return a;
        }
    }
}
