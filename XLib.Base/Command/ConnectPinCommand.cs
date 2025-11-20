using XLib.Node;

namespace XLib.Base.Command
{
    /// <summary>
    /// 连接引脚命令
    /// </summary>
    public class ConnectPinCommand : ICommand
    {
        private readonly PinBase _sourcePin;
        private readonly PinBase _targetPin;

        public string Description => "连接引脚";

        public ConnectPinCommand(PinBase sourcePin, PinBase targetPin)
        {
            _sourcePin = sourcePin;
            _targetPin = targetPin;
        }

        public void Execute()
        {
            // 执行连接
            _sourcePin.TargetList.Add(_targetPin);
            _targetPin.SourceList.Add(_sourcePin);
        }

        public void Undo()
        {
            // 断开连接
            _sourcePin.TargetList.Remove(_targetPin);
            _targetPin.SourceList.Remove(_sourcePin);
        }

        public void Redo()
        {
            Execute();
        }
    }
}