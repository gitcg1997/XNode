using XLib.Node;

namespace XNode.Command
{
    /// <summary>
    /// 断开引脚连接命令
    /// </summary>
    public class DisconnectPinCommand : CommandBase
    {
        private readonly CoreEditer _editer;
        private readonly PinBase _sourcePin;
        private readonly PinBase _targetPin;

        public override string Description => $"断开引脚连接: {_sourcePin.OwnerGroup.OwnerNode.Title} -> {_targetPin.OwnerGroup.OwnerNode.Title}";

        public DisconnectPinCommand(CoreEditer editer, PinBase sourcePin, PinBase targetPin)
        {
            _editer = editer;
            _sourcePin = sourcePin;
            _targetPin = targetPin;
        }

        public override void Execute()
        {
            LogInfo($"执行断开引脚: {_sourcePin.OwnerGroup.OwnerNode.Title} -> {_targetPin.OwnerGroup.OwnerNode.Title}");

            // 断开连接
            _sourcePin.TargetList.Remove(_targetPin);
            _targetPin.SourceList.Remove(_sourcePin);

            // 删除连接线
            _editer.Panel_NodeEditer.RemoveConnectLine(_sourcePin, _targetPin);

            // 更新引脚图标
            _editer.Panel_NodeEditer.UpdateAllPinIcon();
        }

        public override void Undo()
        {
            LogInfo($"撤销断开引脚: {_sourcePin.OwnerGroup.OwnerNode.Title} -> {_targetPin.OwnerGroup.OwnerNode.Title}");

            // 重新连接
            _sourcePin.AddTarget(_targetPin);
            _targetPin.AddSource(_sourcePin);

            // 添加连接线
            _editer.Panel_NodeEditer.AddConnectLine(_sourcePin, _targetPin);

            // 更新引脚图标
            _editer.Panel_NodeEditer.UpdateAllPinIcon();
        }

        public override void Redo()
        {
            LogInfo($"重做断开引脚: {_sourcePin.OwnerGroup.OwnerNode.Title} -> {_targetPin.OwnerGroup.OwnerNode.Title}");
            Execute();
        }
    }
}