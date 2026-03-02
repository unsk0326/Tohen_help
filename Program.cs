using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using DM_Tujen;

class Program
{

    static void Main()
    {

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());       
    }

    public static void FocusPOE()
    {
        IntPtr hWnd = POEhwd.FindWindow(null, "Path of Exile");
        if (hWnd == IntPtr.Zero)
        {
            Form1.BoxWrite("GAME WINDOW NOT FOUND!");
            return;
        }

        POEhwd.MoveWindow(hWnd, 0, 0, 1280, 800, true);
        POEhwd.FocusPoEWindow();
    }

    public static void MainLoop()
    {
        int speed = Form1.Instance.TrackBarValue;
        while (!KeyStop.stopRequested)
        {
            KeyStop.keybd_event(KeyStop.VK_CONTROL, 0, 0, 0);

            if (!Check.IsOpen("DM_Tujen_2.tujenface.png")) // is tujen open
            {
                KeyStop.PressSTwice();
                // Find Tujen until valid position found
                Point? TujenPos;

                do
                {
                    TujenPos = Check.FindImage("DM_Tujen_2.tujenname.png");
                    Thread.Sleep(200); // Avoid CPU spam
                } while (TujenPos == null);

                // Move to Tujen and click
                KeyStop.SetCursorPos(TujenPos.Value.X, TujenPos.Value.Y);
                Thread.Sleep(50);
                KeyStop.LClick(100);
                KeyStop.SetCursorPos(TujenPos.Value.X-500, TujenPos.Value.Y-500);
                //KeyStop.SetCursorPos(0, 0);

                // Wait for Tujen to open (max timeout)
                int waitTime = 0;
                while (!Check.IsOpen("DM_Tujen_2.tujenface.png") && waitTime < 2000)
                {
                    Thread.Sleep(250);
                    waitTime += 250;
                }
            }
            
            Span<Point> inventoryCount = Check.Inventory3();
            //Console.WriteLine("itemcount: " + itemCount.Count);

            if (inventoryCount.Length >= 50)
            {
                
                //Console.WriteLine($"isStashopen: {Check.IsStashOpen()}");

                Point? stashPos = null;
                int waitTime = 0;
                do
                {
                    KeyStop.PressSTwice();
                    stashPos = Check.FindImage("DM_Tujen_2.stash.png");
                    waitTime += 100;
                    Thread.Sleep(100); // Wait a bit before retrying
                } while (stashPos == null && waitTime <= 500);

                if (stashPos == null)
                {
                    KeyStop.SetCursorPos(632, 173);
                    Thread.Sleep(100);
                    KeyStop.LClick(100);
                    Thread.Sleep(500);
                    stashPos = Check.FindImage("DM_Tujen_2.stash.png");
                }

                KeyStop.SetCursorPos(stashPos.Value.X, stashPos.Value.Y);
                Thread.Sleep(50);
                KeyStop.LClick(100);
                KeyStop.SetCursorPos(stashPos.Value.X-500, stashPos.Value.Y-500);
                //Console.WriteLine("Clicked on stash.");

                // Wait for stash to fully open
                waitTime = 0;
                while (!Check.IsOpen("DM_Tujen_2.stashface.png") && waitTime < 2000) // Max wait 2 seconds
                {
                    Thread.Sleep(250);
                    waitTime += 250;
                    //Console.WriteLine("Waiting for stash to open...");
                }

                //Console.WriteLine("Stash is open!");

                // Move each item into stash
                foreach (var pos in inventoryCount)
                {
                    if (KeyStop.stopRequested) break;
                    if (Check.IsOpen("DM_Tujen_2.stashface.png"))
                    {
                        // Wait between clicks
                        KeyStop.SetCursorPos(pos.X, pos.Y);
                        Thread.Sleep(20);
                        KeyStop.LClick(50);
                    }
                    else break;
                    //Console.WriteLine($"Moved item at {pos.X}, {pos.Y} to stash.");
                }
                KeyStop.PressSTwice();
            }

            if (Check.IsOpen("DM_Tujen_2.tujenface.png"))
            {
                //Console.WriteLine("bat dau kiem tra item tujen");
                List<Point> tujenItemCount = Check.TujenItemPos();
                //Console.WriteLine("tujenItemcount: " + tujenItemCount.Count);


                foreach (var pos in tujenItemCount)
                {
                    if (KeyStop.stopRequested) break;
                    KeyStop.SetCursorPos(pos.X, pos.Y);
                    Thread.Sleep(30 * speed);
                    KeyStop.PressC();
                    Thread.Sleep(25 * speed);

                    string itemData = ClipboardRead.GetClipboardTextUltraFast();

                    // Extract item info from Clipboard
                    ItemDetails item = ClipboardRead.ExtractBasicItemDetails(itemData);
                    //stopwatch.Stop();
                    if (item.IsGem)
                    {
                        ClipboardRead.ExtractGemDetails(itemData, item);
                    }

                    string itemInfo = $"{item.ItemName}" +
                          (item.IsGem ? $" | L: {item.GemLevel} | Q: {item.GemQuality}%" : "");

                    Form1.BoxWrite(itemInfo);
                    Logger.WriteLog(itemInfo);

                    if ((item.ItemClass == "Skill Gems" || item.ItemClass == "Support Gems") &&
                                            item.GemLevel == "21" && item.GemQuality == "20" && Form1.Instance.checkBox1.Checked)
                    {
                        //Console.WriteLine("Gem 21/20, buying now!");
                        Logger.WriteLog(" -> BUY");
                        KeyStop.BuyItem();
                    }
                    else if (Check.IsItemWhitelisted(item.ItemName))
                    {
                        //Console.WriteLine("Item in whitelist, buying now!");
                        Logger.WriteLog(" -> BUY");
                        KeyStop.BuyItem();
                    };
                }

                if (Check.isOutOfCoin()) KeyStop.stopAndCtrlUp();

                if (!KeyStop.stopRequested)
                {
                    Form1.BoxWrite("--------------------REFRESH-------------------");
                    Logger.WriteLog("--------------------REFRESH-------------------");
                    KeyStop.SetCursorPos(630, 645);
                    Thread.Sleep(20 * speed);
                    KeyStop.LClick(50);
                    Thread.Sleep(20 * speed);
                }
            }
            

        }
    }
}