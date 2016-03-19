using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NowPlaying
{
    public partial class Form1 : Form
    {
        private readonly CustomWindow cw;
        private readonly string savePath;
        public Form1()
        {
            InitializeComponent();
            cw = new CustomWindow("MsnMsgrUIManager", WriteFile);
            savePath = Path.Combine(Application.StartupPath, "now_playing.txt");
            txtPath.Text = savePath;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            cw.Dispose();
        }

        private void WriteFile(string s)
        {
            string[] separators = { @"\0" };
            string[] result = s.Split(separators, StringSplitOptions.None);
            txtArtist.Text = result[5];
            txtTitle.Text = result[4];
            File.WriteAllText(savePath, $"{result[5]} - {result[4]}");

            foreach (string x in result)
            {
                txtDebug.AppendText(x + "\r\n");
            }
        }
    }
    // To catch the MSN messenger Now Playing messages we need a window with class name 'MsnMsgrUIManager'
    // Winforms and WPF window class names are auto-generated and cannot be manually set
    // So we create a hidden win32 window with the class name and set it to capture any WM_COPYDATA messages sent to it
    // Parse those messages and write the relevant data to a text file.
    internal class CustomWindow : IDisposable
    {
        delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WNDCLASS
        {
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern ushort RegisterClassW([In] ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr CreateWindowExW(
           UInt32 dwExStyle,
           [MarshalAs(UnmanagedType.LPWStr)]
       string lpClassName,
           [MarshalAs(UnmanagedType.LPWStr)]
       string lpWindowName,
           UInt32 dwStyle,
           Int32 x,
           Int32 y,
           Int32 nWidth,
           Int32 nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam
        );

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyWindow(IntPtr hWnd);

        private const int ERROR_CLASS_ALREADY_EXISTS = 1410;

        private bool m_disposed;
        private IntPtr m_hwnd;

        private const uint WM_COPYDATA = 0x004A;

        private static Action<string> update;
        
        // Don't let it be garbage collected
        private WndProc m_wnd_proc_delegate;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                // Dispose unmanaged resources
                if (m_hwnd != IntPtr.Zero)
                {
                    DestroyWindow(m_hwnd);
                    m_hwnd = IntPtr.Zero;
                }

            }
        }

        public CustomWindow(string class_name, Action<string> u)
        {

            if (class_name == null) throw new Exception("class_name is null");
            if (class_name == String.Empty) throw new Exception("class_name is empty");
            update = u;
            m_wnd_proc_delegate = CustomWndProc;

            // Create WNDCLASS
            WNDCLASS wind_class = new WNDCLASS();
            wind_class.lpszClassName = class_name;
            wind_class.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(m_wnd_proc_delegate);

            UInt16 class_atom = RegisterClassW(ref wind_class);

            int last_error = Marshal.GetLastWin32Error();

            if (class_atom == 0 && last_error != ERROR_CLASS_ALREADY_EXISTS)
            {
                throw new Exception("Could not register window class");
            }

            // Create window
            m_hwnd = CreateWindowExW(
                0,
                class_name,
                String.Empty,
                0,
                0,
                0,
                0,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero
            );
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpData;
        }

        private static IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                // grab the copydata message, extract the message contents and send them to the main form
                case WM_COPYDATA:
                    CopyDataStruct message = (CopyDataStruct)Marshal.PtrToStructure(lParam, typeof(CopyDataStruct));
                    update(message.lpData);
                    break;
            }
            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }
    }
}