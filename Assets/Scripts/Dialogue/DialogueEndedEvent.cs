namespace Game.Dialogue
{
    /// <summary>一段对话结束（正常播完，或被前置校验拦下）；订阅者据此解锁后续流程。</summary>
    public readonly struct DialogueEndedEvent
    {
        public DialogueEndedEvent(DialogueTrigger trigger, bool completed)
        {
            Trigger = trigger;
            Completed = completed;
        }

        /// <summary>结束的这段对话所在的对象，订阅者用来判断是不是自己那段。</summary>
        public DialogueTrigger Trigger { get; }

        /// <summary>true 表示完整播到结束；false 表示校验未通过或中途抛异常。</summary>
        public bool Completed { get; }
    }
}
