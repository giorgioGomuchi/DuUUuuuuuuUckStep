using System;
using UnityEngine;

public interface ISequenceActorAdapter
{
    bool IsValid { get; }

    event Action<SequenceFailReason> OnExternalSequenceFail;

    void Bind();
    void Unbind();

    void OnSequenceStarted(SequenceDefinitionSOBase definition);
    void OnSequenceCancelled();
    void OnSequenceFailed(SequenceFailReason reason);
    void OnSequenceCompleted();

    void EnterStepWindow(int stepIndex);
    void ExitStepWindow(int stepIndex);

    void TickSequence(float deltaTime);

    SequenceActionResult TryHandlePrimaryAction(float normalizedWindowTime);
    SequenceActionResult TryHandleSecondaryAction(float normalizedWindowTime);
    SequenceActionResult TryHandleDashAction(float normalizedWindowTime);

    void ApplyReward(SequenceRewardSOBase reward, SequenceRewardResolution resolution, SequenceRewardContextBase context);
}