using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PonyShots4Win
{
    public class HotKeys
    {
        public delegate void HotKeyEventHandler(Keys key, KeyModifier modifiers);
        public event HotKeyEventHandler OnHotKey;

        private int _hotkeyIdx = 0;
        private readonly IntPtr _handle;

        public HotKeys(IntPtr handle)
        {
            _handle = handle;
        }

        public void RegisterHotKey(Keys key, params KeyModifier[] modifiers)
        {
            var modifierBitmask = modifiers.Aggregate(0, (current, mod) => current | (int) mod);

            WinApi.RegisterHotKey(_handle, _hotkeyIdx, modifierBitmask, key.GetHashCode());
            _hotkeyIdx++;
        }

        public void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                var key = (Keys) (((int) m.LParam >> 16) & 0xFFFF); // The key of the hotkey that was pressed.
                var modifier = (KeyModifier) ((int) m.LParam & 0xFFFF); // The modifier of the hotkey that was pressed.
                var id = m.WParam.ToInt32(); // The id of the hotkey that was pressed.

                OnHotKey?.Invoke(key, modifier);
            }
        }
    }
}
