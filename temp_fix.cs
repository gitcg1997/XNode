        public void EndDragNode()
        {
            _host.OperateArea.Cursor = _tool.Cursor;

            // 获取核心编辑器以执行命令
            var coreEditer = _host.Parent as CoreEditer;
            if (coreEditer != null)
            {
                // 为每个移动的节点记录旧位置和新位置
                foreach (var card in GetComponent<CardComponent>().SelectedCardList)
                {
                    // 保存移动前的节点位置
                    var oldPosition = new XLib.Node.NodePoint(card.NodeInstance.Point.X, card.NodeInstance.Point.Y);

                    // 应用偏移，获取新位置
                    card.ApplyOffset();
                    var newPosition = new XLib.Node.NodePoint(card.NodeInstance.Point.X, card.NodeInstance.Point.Y);

                    // 只有在位置真正发生变化时才执行命令
                    if (oldPosition.X != newPosition.X || oldPosition.Y != newPosition.Y)
                    {
                        // 先恢复到旧位置,让命令来执行移动
                        card.NodeInstance.Point.X = oldPosition.X;
                        card.NodeInstance.Point.Y = oldPosition.Y;

                        // 执行移动节点命令，以支持撤销/重做
                        coreEditer.ExecuteMoveNodeCommand(card.NodeInstance, oldPosition, newPosition);
                    }
                }
            }
            else
            {
                // 如果无法获取核心编辑器，则直接应用偏移
                foreach (var card in GetComponent<CardComponent>().SelectedCardList) card.ApplyOffset();
            }
        }
