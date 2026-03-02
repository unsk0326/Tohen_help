using System;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Buffers;
using System.Reflection;

public class Check
{
    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

    public static System.Drawing.Point? FindImage(string imagePath, double threshold = 0.8)
    {
        //string resourceName = "DM_Tujen_2.tujenname.png"; // Template image to find
        int width = 1280, height = 800;     // Search area size

        // Define capture region (starting from top-left corner of screen)
        Rectangle captureRegion = new Rectangle(0, 0, width, height);

        // Capture screen region
        Bitmap screenshot = CaptureRegion(captureRegion);
        using Mat screenMat = BitmapConverter.ToMat(screenshot); // Convert Bitmap to Mat

        // Read image from embedded resource
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(imagePath);
        if (stream == null) return null; // Resource not found, return null
        using Bitmap templateBitmap = new Bitmap(stream);
        using Mat template = BitmapConverter.ToMat(templateBitmap); // Convert Bitmap to Mat

        // Convert screen image to grayscale if not already
        using Mat grayScreen = new Mat();
        Cv2.CvtColor(screenMat, grayScreen, ColorConversionCodes.BGR2GRAY);
        using Mat grayTemplate = new Mat();
        Cv2.CvtColor(template, grayTemplate, ColorConversionCodes.BGR2GRAY);

        // Perform Template Matching
        using Mat result = new Mat();
        Cv2.MatchTemplate(grayScreen, grayTemplate, result, TemplateMatchModes.CCoeffNormed);

        // Find the position with highest match
        Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

        // If accuracy is above threshold, return center coordinates of found image
        if (maxVal >= threshold)
        {
            int centerX = maxLoc.X + template.Width / 2;
            int centerY = maxLoc.Y + template.Height / 2;
            return new System.Drawing.Point(centerX, centerY);
        }

        return null; // Image not found
    }


    public static bool IsOpen(string templatePath, double threshold = 0.8)
    {
        //string templatePath = "DM_Tujen_2.tujenface.png"; // Template image to find
        int width = 1280, height = 800;     // Search area size

        // Define capture region (starting from top-left corner of screen)
        Rectangle captureRegion = new Rectangle(0, 0, width, height);

        // Capture screen region
        Bitmap screenshot = CaptureRegion(captureRegion);
        using Mat screenMat = BitmapConverter.ToMat(screenshot); // Convert Bitmap to Mat
        //using Mat template = Cv2.ImRead(templatePath, ImreadModes.Grayscale); // Read template as grayscale

        // Read image from embedded resource
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(templatePath);
        if (stream == null) return false; // Resource not found, return false
        using Bitmap templateBitmap = new Bitmap(stream);
        using Mat template = BitmapConverter.ToMat(templateBitmap); // Convert Bitmap to Mat

        // Convert screen image to grayscale if not already
        using Mat grayScreen = new Mat();
        Cv2.CvtColor(screenMat, grayScreen, ColorConversionCodes.BGR2GRAY);
        using Mat grayTemplate = new Mat();
        Cv2.CvtColor(template, grayTemplate, ColorConversionCodes.BGR2GRAY);


        // Perform Template Matching
        using Mat result = new Mat();
        Cv2.MatchTemplate(grayScreen, grayTemplate, result, TemplateMatchModes.CCoeffNormed);

        // Find the position with highest match
        Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

        // If accuracy is above threshold, return true
        if (maxVal >= threshold)
        {
            return true;
        }
        return false; // Image not found
    }

    public static Span<System.Drawing.Point> Inventory3()
    {
        int cols = 12, rows = 5;
        int slotWidth = 37, slotHeight = 37;
        int startX = 815, startY = 445;
        Color backgroundColor = Color.FromArgb(4, 6, 6);

        using Bitmap screenshot = new Bitmap(cols * slotWidth, rows * slotHeight);
        using Graphics g = Graphics.FromImage(screenshot);
        g.CopyFromScreen(startX, startY, 0, 0, screenshot.Size);

        BitmapData bmpData = screenshot.LockBits(new Rectangle(0, 0, screenshot.Width, screenshot.Height),
                                                 ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        int stride = bmpData.Stride;
        int bytes = stride * screenshot.Height;
        byte[] pixelData = ArrayPool<byte>.Shared.Rent(bytes); // Use ArrayPool to avoid new allocation
        Marshal.Copy(bmpData.Scan0, pixelData, 0, bytes);
        screenshot.UnlockBits(bmpData);

        // Use ArrayPool to avoid List<> allocation
        System.Drawing.Point[] itemBuffer = ArrayPool<System.Drawing.Point>.Shared.Rent(rows * cols);
        int itemCount = 0;

        Parallel.For(0, rows, row =>
        {
            for (int col = 0; col < cols; col++)
            {
                int centerX = startX + col * slotWidth + slotWidth / 2;
                int centerY = startY + row * slotHeight + slotHeight / 2;
                Color pixelColor = GetPixelFast(pixelData, (col * slotWidth) + slotWidth / 2,
                                                         (row * slotHeight) + slotHeight / 2, stride);

                if (!IsSameColor(pixelColor, backgroundColor))
                {
                    int index = Interlocked.Increment(ref itemCount) - 1;
                    itemBuffer[index] = new System.Drawing.Point(centerX, centerY);
                }
            }
        });

        ArrayPool<byte>.Shared.Return(pixelData); // Return memory to pool

        // Sort top-to-bottom, left-to-right
        Array.Sort(itemBuffer, 0, itemCount, Comparer<System.Drawing.Point>.Create((p1, p2) =>
        {
            int cmpX = p1.X.CompareTo(p2.X); // Compare row first
            return cmpX != 0 ? cmpX : p1.Y.CompareTo(p2.Y); // If same row, compare column
        }));

        return new Span<System.Drawing.Point>(itemBuffer, 0, itemCount);
    }

    public static List<System.Drawing.Point> TujenItemPos()
    {
        // Inventory size
        int cols = 2, rows = 11; // 2x11 slots
        int slotWidth = 37, slotHeight = 37; // Each slot is 37x37 pixels
        int startX = 183, startY = 216; // Top-left corner of inventory on screen

        // Background color for comparison
        Color backgroundColor = Color.FromArgb(4, 6, 6); // Background color

        // Capture inventory screenshot
        using Bitmap screenshot = new Bitmap(cols * slotWidth, rows * slotHeight);
        using Graphics g = Graphics.FromImage(screenshot);
        g.CopyFromScreen(startX, startY, 0, 0, screenshot.Size);

        // Use LockBits for faster pixel access
        BitmapData bmpData = screenshot.LockBits(new Rectangle(0, 0, screenshot.Width, screenshot.Height),
                                                 ImageLockMode.ReadOnly,
                                                 PixelFormat.Format24bppRgb);

        int stride = bmpData.Stride;
        int bytes = stride * screenshot.Height;
        byte[] pixelData = new byte[bytes];
        Marshal.Copy(bmpData.Scan0, pixelData, 0, bytes);
        screenshot.UnlockBits(bmpData);


        // List of screen coordinates of slots containing items
        List<System.Drawing.Point> itemPositions = new List<System.Drawing.Point>();

        // Use Parallel.For for multi-threaded checking
        Parallel.For(0, rows, row =>
        {
            for (int col = 0; col < cols; col++)
            {
                int centerX = startX + col * slotWidth + slotWidth / 2; // Screen X position
                int centerY = startY + row * slotHeight + slotHeight / 2; // Screen Y position
                Color pixelColor = GetPixelFast(pixelData, (col * slotWidth) + slotWidth / 2,
                                                         (row * slotHeight) + slotHeight / 2, stride);

                // If color doesn't match background, there's an item
                if (!IsSameColor(pixelColor, backgroundColor))
                {
                    lock (itemPositions) // Ensure thread safety
                    {
                        itemPositions.Add(new System.Drawing.Point(centerX, centerY)); // Save screen coordinates
                    }
                }
            }
        });
        itemPositions = itemPositions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        return itemPositions;
    }

    public static bool isConfirmOn()
    {
        IntPtr hdc = GetDC(IntPtr.Zero);

        try
        {
            Color color1 = GetPixelColor(hdc, 312, 341);
            Color color2 = GetPixelColor(hdc, 490, 341);

            Color expectedColor1 = Color.FromArgb(29, 28, 28);
            Color expectedColor2 = Color.FromArgb(28, 26, 26);

            return (color1 == expectedColor1 && color2 == expectedColor2);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }
    }

    public static bool isOutOfArtf()
    {
        IntPtr hdc = GetDC(IntPtr.Zero);

        try
        {
            Color color1 = GetPixelColor(hdc, 358, 650);
            Color color2 = GetPixelColor(hdc, 442, 650);

            Color expectedColor1 = Color.FromArgb(66, 66, 66);
            Color expectedColor2 = Color.FromArgb(67, 67, 67);

            return (color1 == expectedColor1 && color2 == expectedColor2);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }
    }

    public static bool isOutOfCoin()
    {
        IntPtr hdc = GetDC(IntPtr.Zero);

        try
        {
            Color color1 = GetPixelColor(hdc, 619, 660);
            Color color2 = GetPixelColor(hdc, 641, 660);

            Color expectedColor1 = Color.FromArgb(51, 51, 51);
            Color expectedColor2 = Color.FromArgb(47, 47, 47);

            return (color1 == expectedColor1 && color2 == expectedColor2);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }
    }

    public static bool IsItemWhitelisted(string itemName)
    {
        return Whitelist.list.Contains(itemName);
    }

    public static Bitmap CaptureRegion(Rectangle region)
    {
        Bitmap bitmap = new Bitmap(region.Width, region.Height);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(region.Location, System.Drawing.Point.Empty, region.Size);
        }
        return bitmap;
    }

    private static Color GetPixelColor(IntPtr hdc, int x, int y)
    {
        uint pixel = GetPixel(hdc, x, y);
        int r = (int)(pixel & 0x000000FF);
        int g = (int)((pixel & 0x0000FF00) >> 8);
        int b = (int)((pixel & 0x00FF0000) >> 16);
        return Color.FromArgb(r, g, b);
    }

    // Get color from Bitmap byte array
    static Color GetPixelFast(byte[] pixelData, int x, int y, int stride)
    {
        int index = (y * stride) + (x * 3);
        return Color.FromArgb(pixelData[index + 2], pixelData[index + 1], pixelData[index]);
    }

    // Compare if two colors are similar (with small tolerance)
    static bool IsSameColor(Color a, Color b, int tolerance = 10)
    {
        return Math.Abs(a.R - b.R) < tolerance &&
               Math.Abs(a.G - b.G) < tolerance &&
               Math.Abs(a.B - b.B) < tolerance;
    }
}
