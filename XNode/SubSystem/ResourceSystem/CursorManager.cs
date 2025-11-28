using System.Windows;
using System.Windows.Input;
using System.Windows.Resources;
using WpfCursor = System.Windows.Input.Cursor;

namespace XNode.SubSystem.ResourceSystem
{
    /// <summary>
    /// 光标管理器
    /// </summary>
    public class CursorManager
    {
        #region 单例

        private CursorManager() { }
        public static CursorManager Instance { get; } = new CursorManager();

        #endregion

        #region 光标

        /// <summary>选择</summary>
        public WpfCursor? Select { get; set; }

        /// <summary>选择并移动</summary>
        public WpfCursor? SelectAndMove { get; set; }

        /// <summary>移动</summary>
        public WpfCursor? Move { get; set; }

        /// <summary>水平移动</summary>
        public WpfCursor? MoveX { get; set; }

        /// <summary>垂直移动</summary>
        public WpfCursor? MoveY { get; set; }

        /// <summary>十字</summary>
        public WpfCursor? Cross { get; set; }

        /// <summary>插入</summary>
        public WpfCursor? Insert { get; set; }

        /// <summary>绘制</summary>
        public WpfCursor? Draw { get; set; }

        /// <summary>禁止</summary>
        public WpfCursor? Disable { get; set; }

        /// <summary>移至顶端</summary>
        public WpfCursor? MoveTop { get; set; }

        /// <summary>移至底端</summary>
        public WpfCursor? MoveBottom { get; set; }

        /// <summary>缩放：左上至右下</summary>
        public WpfCursor? ResizeUpDown { get; set; }

        /// <summary>缩放：左下至右上</summary>
        public WpfCursor? ResizeDownUp { get; set; }

        /// <summary>开关</summary>
        public WpfCursor? OnOff { get; set; }

        #endregion

        #region 管理器接口

        public void Init()
        {
            Select = LoadCursor("Assets/Cursor/Select.cur");
            SelectAndMove = LoadCursor("Assets/Cursor/MoveSelected.cur");
            Move = LoadCursor("Assets/Cursor/Move.cur");
            MoveX = LoadCursor("Assets/Cursor/MoveX.cur");
            MoveY = LoadCursor("Assets/Cursor/MoveY.cur");
            Cross = LoadCursor("Assets/Cursor/Cross.cur");
            Insert = LoadCursor("Assets/Cursor/Insert.cur");
            Draw = LoadCursor("Assets/Cursor/Draw.cur");
            Disable = LoadCursor("Assets/Cursor/Disable.cur");
            MoveTop = LoadCursor("Assets/Cursor/MoveTop.cur");
            MoveBottom = LoadCursor("Assets/Cursor/MoveBottom.cur");
            ResizeUpDown = LoadCursor("Assets/Cursor/ResizeUpDown.cur");
            ResizeDownUp = LoadCursor("Assets/Cursor/ResizeDownUp.cur");
            OnOff = LoadCursor("Assets/Cursor/OnOff.cur");
        }

        #endregion

        #region 私有方法

        private WpfCursor LoadCursor(string cursorPath)
        {
            StreamResourceInfo resourceInfo = Application.GetResourceStream(new Uri(cursorPath, UriKind.Relative));
            return new WpfCursor(resourceInfo.Stream);
        }

        #endregion
    }
}