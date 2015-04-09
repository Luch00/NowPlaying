using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NowPlaying
{
    public partial class Form1 : Form
    {
        private CustomWindow cw;
        private Action<string> update;
        public Form1()
        {
            InitializeComponent();
            update = WriteFile;
            cw = new CustomWindow("MsnMsgrUIManager", update);
            txtPath.Text = Application.StartupPath + @"\now_playing.txt";
        }

        private void WriteFile(string s)
        {
            string[] separators = new string[] { @"\0" };
            string[] result = s.Split(separators, StringSplitOptions.None);
            txtArtist.Text = result[5];
            txtTitle.Text = result[4];
            System.IO.File.WriteAllText(Application.StartupPath + @"\now_playing.txt", result[5] + " - " + result[4]);

            foreach (string x in result)
            {
                txtDebug.Text += x + "\r\n";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            cw.Dispose();
        }
    }

    class CustomWindow : IDisposable
    {
        delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.StructLayout(
            System.Runtime.InteropServices.LayoutKind.Sequential,
           CharSet = System.Runtime.InteropServices.CharSet.Unicode
        )]
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
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string lpszClassName;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern System.UInt16 RegisterClassW(
            [System.Runtime.InteropServices.In] ref WNDCLASS lpWndClass
        );

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr CreateWindowExW(
           UInt32 dwExStyle,
           [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
       string lpClassName,
           [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
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

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern System.IntPtr DefWindowProcW(
            IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam
        );

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyWindow(
            IntPtr hWnd
        );

        private const int ERROR_CLASS_ALREADY_EXISTS = 1410;

        private bool m_disposed;
        private IntPtr m_hwnd;

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
            update = u;
            if (class_name == null) throw new System.Exception("class_name is null");
            if (class_name == String.Empty) throw new System.Exception("class_name is empty");

            m_wnd_proc_delegate = CustomWndProc;

            // Create WNDCLASS
            WNDCLASS wind_class = new WNDCLASS();
            wind_class.lpszClassName = class_name;
            wind_class.lpfnWndProc = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(m_wnd_proc_delegate);

            UInt16 class_atom = RegisterClassW(ref wind_class);

            int last_error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

            if (class_atom == 0 && last_error != ERROR_CLASS_ALREADY_EXISTS)
            {
                throw new System.Exception("Could not register window class");
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

        private static IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {          
            switch (msg)
            {
                case WM_COPYDATA:
                    CopyDataStruct message = (CopyDataStruct)Marshal.PtrToStructure(lParam, typeof(CopyDataStruct));
                    update(message.lpData);
                    break;
                default:
                    break;
            }
            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] 
        public struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpData;
        }
        const uint WM_COPYDATA = 0x004A;
        static Action<string> update;
        private WndProc m_wnd_proc_delegate;
    }
}
