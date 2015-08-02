using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NowPlaying
{
    public partial class Form1 : Form
    {
        private readonly CustomWindow cw;

        public Form1()
        {
            InitializeComponent();
            cw = new CustomWindow("MsnMsgrUIManager", WriteFile);
            txtPath.Text = Application.StartupPath + @"\now_playing.txt";
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
            System.IO.File.WriteAllText(Application.StartupPath + @"\now_playing.txt", result[5] + @" - " + result[4]);

            foreach (string x in result)
            {
                txtDebug.Text += x + "\r\n";
            }
        }
    }

    internal class CustomWindow : IDisposable
    {
        private const int ERROR_CLASS_ALREADY_EXISTS = 1410;

        private const uint WM_COPYDATA = 0x004A;

        private static Action<string> update;

        private bool m_disposed;

        private IntPtr m_hwnd;

        public CustomWindow(string class_name, Action<string> u)
        {
            update = u;
            if (class_name == null) throw new Exception("class_name is null");
            if (class_name == string.Empty) throw new Exception("class_name is empty");

            // Create WNDCLASS
            WNDCLASS wind_class = new WNDCLASS
            {
                lpszClassName = class_name,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate((WndProc)CustomWndProc)
            };

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
                string.Empty,
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

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowExW(
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

        private static IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_COPYDATA:
                    CopyDataStruct message = (CopyDataStruct)Marshal.PtrToStructure(lParam, typeof(CopyDataStruct));
                    update(message.lpData);
                    break;
            }
            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern ushort RegisterClassW([In] ref WNDCLASS lpWndClass);

        private void Dispose(bool disposing)
        {
            if (m_disposed) return;
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpData;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASS
        {
            private readonly uint style;
            public IntPtr lpfnWndProc;
            private readonly int cbClsExtra;
            private readonly int cbWndExtra;
            private readonly IntPtr hInstance;
            private readonly IntPtr hIcon;
            private readonly IntPtr hCursor;
            private readonly IntPtr hbrBackground;

            [MarshalAs(UnmanagedType.LPWStr)]
            private readonly string lpszMenuName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
        }
    }
}