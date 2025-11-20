using XLib.Node;

namespace XLib.Base.Command
{
    /// <summary>
    /// 断开引脚连接命令
    /// </summary>
    public class DisconnectPinCommand : ICommand
    {
        private readonly PinBase _sourcePin;
        private readonly PinBase _targetPin;

        public string Description => "断开引脚连接";

        public DisconnectPinCommand(PinBase sourcePin, PinBase targetPin)
        {
            _sourcePin = sourcePin;
            _targetPin = targetPin;
        }

        public void Execute()
        {
            // 断开连接
            _sourcePin.TargetList.Remove(_targetPin);
            _targetPin.SourceList.Remove(_sourcePin);
        }

        public void Undo()
        {
            // 重新连接
            _sourcePin.TargetList.Add(_targetPin);
            _targetPin.SourceList.Add(_sourcePin);
        }

        public void Redo()
        {
            Execute();
        }
    }
}