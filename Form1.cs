using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Timers;

namespace DM_Tujen
{
    public partial class Form1 : Form
    {
        public static Form1 Instance { get; private set; }

        public static Thread mainThread;
        public static Thread hotkeyThread;
        //public System.Windows.Forms.Timer timer1;
        private DateTime startTime;

        HashSet<string> items = Whitelist.list;
        public Form1()
        {
            InitializeComponent();
            Instance = this; // Save Form1 instance for static method access
            TrackBarValue = trackBar1.Value; // Set initial value
            // Create Timer
            timer1.Interval = 1000; // Update every second
            timer1.Tick += timer1_Tick;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string item = textBox1.Text.Trim();

            if (!string.IsNullOrEmpty(item) && !Whitelist.list.Contains(item))
            {
                Whitelist.list.Add(item);
                Whitelist.SaveToFile(); // Save list to file immediately
                UpdateListBox(); // Refresh ListBox
                textBox1.Clear();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            // Register V key hotkey (Virtual-Key: 0x56)
            KeyStop.RegisterHotKey(IntPtr.Zero, 1, KeyStop.MOD_NOREPEAT, 0x56);

            KeyStop.stopRequested = false;

            Program.FocusPOE();

            hotkeyThread = new Thread(KeyStop.HotKeyListener);
            hotkeyThread.IsBackground = true;
            hotkeyThread.Start(); // Start V key listener thread

            startTime = DateTime.Now; // Save start time
            timer1.Start(); // Start the timer

            // Create main thread
            mainThread = new Thread(Program.MainLoop);
            mainThread.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            Whitelist.LoadFromFile(); // Load data from file
            listBox1.Items.AddRange(Whitelist.list.ToArray()); // Populate ListBox
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) // Check if Delete key was pressed
            {
                if (listBox1.SelectedItem != null) // Check if an item is selected
                {
                    Whitelist.list.Remove(listBox1.SelectedItem.ToString()); // Remove item from list
                    listBox1.Items.Remove(listBox1.SelectedItem); // Remove selected item
                    Whitelist.SaveToFile(); // Save list to file immediately
                    UpdateListBox(); // Refresh ListBox
                }
            }
        }

        private void UpdateListBox()
        {
            listBox1.Items.Clear(); // Clear the entire old list
            listBox1.Items.AddRange(Whitelist.list.ToArray()); // Refresh list from HashSet
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public static void BoxWrite(string message)
        {
            if (Instance?.listBox2.InvokeRequired == true)
            {
                Instance.listBox2.Invoke(new Action(() =>
                {
                    Instance.listBox2.Items.Add(message);
                    Instance.listBox2.TopIndex = Instance.listBox2.Items.Count - 1; // Ensure scroll to bottom
                }));
            }

            else
            {
                //Instance.listBox2.SelectedIndex = Instance.listBox2.Items.Count - 1;
                Instance.listBox2.Items.Add(message);
                Instance.listBox2.TopIndex = Instance.listBox2.Items.Count - 1;
            }
        }

        public void timer1_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            label2.Text = $"🕛 {elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        public void label2_Click_1(object sender, EventArgs e)
        {

        }

        public void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }


        public int TrackBarValue { get; private set; }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label3.Text = $"Speed: {trackBar1.Value}";
            TrackBarValue = trackBar1.Value;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
