using XLib.Node;

namespace XNode.Command
{
    /// <summary>
    /// 连接引脚命令
    /// </summary>
    public class ConnectPinCommand : CommandBase
    {
        private readonly CoreEditer _editer;
        private readonly PinBase _sourcePin;
        private readonly PinBase _targetPin;
        private PinBase? _oldConnection; // 保存数据引脚的旧连接

        public override string Description => $"连接引脚: {_sourcePin.OwnerGroup.OwnerNode.Title} -> {_targetPin.OwnerGroup.OwnerNode.Title}";

        public ConnectPinCommand(CoreEditer editer, PinBase sourcePin, PinBase targetPin)
        {
            _editer = editer;
            _sourcePin = sourcePin;
            _targetPin = targetPin;

            // 如果目标是数据引脚且已有连接,保存旧连接
            if (_targetPin is DataPin && _targetPin.SourceList.Count > 0)
            {
                _oldConnection = _targetPin.SourceList[0];
            }
        }

        public override void Execute()
        {
            LogInfo($"执行连接引脚: {_sourcePin.OwnerGroup.OwnerNode.Title} -> {_targetPin.OwnerGroup.OwnerNode.Title}");

            // 如果是数据引脚,先断开旧连接
            if (_targetPin is DataPin && _oldConnection != null)
            {
                _oldConnection.TargetList.Remove(_targetPin);
                _targetPin.SourceList.Remove(_oldConnection);
                _editer.Panel_NodeEditer.RemoveConnectLine(_oldConnection, _targetPin);
            }

            // 执行连接
            _sourcePin.AddTarget(_targetPin);
            _targetPin.AddSource(_sourcePin);

            // 添加连接线到UI
            _editer.Panel_NodeEditer.AddConnectLine(_sourcePin, _targetPin);

            // 更新引脚图标
            _editer.Panel_NodeEditer.UpdateAllPinIcon();
        }

        public override void Undo()
        {
            LogInfo($"撤销连接引脚: {_sourcePin.OwnerGroup.OwnerNode.Title} -> {_targetPin.OwnerGroup.OwnerNode.Title}");

            // 断开连接
            _sourcePin.TargetList.Remove(_targetPin);
            _targetPin.SourceList.Remove(_sourcePin);

            // 删除连接线
            _editer.Panel_NodeEditer.RemoveConnectLine(_sourcePin, _targetPin);

            // 如果有旧连接,恢复它
            if (_oldConnection != null)
            {
                _oldConnection.AddTarget(_targetPin);
                _targetPin.AddSource(_oldConnection);
                _editer.Panel_NodeEditer.AddConnectLine(_oldConnection, _targetPin);
            }

            // 更新引脚图标
            _editer.Panel_NodeEditer.UpdateAllPinIcon();
        }

        public override void Redo()
        {
            LogInfo($"重做连接引脚: {_sourcePin.OwnerGroup.OwnerNode.Title} -> {_targetPin.OwnerGroup.OwnerNode.Title}");
            Execute();
        }
    }
}