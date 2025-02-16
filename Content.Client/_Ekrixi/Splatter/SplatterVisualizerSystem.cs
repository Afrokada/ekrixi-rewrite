using Content.Shared._Ekrixi.Splatter;
using Content.Shared.Throwing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._Ekrixi.Splatter;

/// <summary>
///     Handles animating splatting items.
/// </summary>
public sealed class SplatterVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;

    private const string AnimationKey = "splat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SplatterComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
        SubscribeLocalEvent<SplatterComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAutoHandleState(EntityUid uid, SplatterComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(uid);

        if (_anim.HasRunningAnimation(uid, animationPlayer, AnimationKey))
            return;

        var anim = GetAnimation((uid, component, sprite));
        if (anim == null)
            return;

        component.OriginalScale = sprite.Scale;
        _anim.Play((uid, animationPlayer), anim, AnimationKey);
    }

    private void OnShutdown(EntityUid uid, SplatterComponent component, ComponentShutdown args)
    {
        if (!_anim.HasRunningAnimation(uid, AnimationKey))
            return;

        if (TryComp<SpriteComponent>(uid, out var sprite) && component.OriginalScale != null)
            sprite.Scale = component.OriginalScale.Value;

        _anim.Stop(uid, AnimationKey);
    }

    private static Animation? GetAnimation(Entity<SplatterComponent, SpriteComponent> ent)
    {
        if (ent.Comp1.StopFallTime - ent.Comp1.StartFallTime is not { } length)
            return null;

        if (length <= TimeSpan.Zero)
            return null;

        length += TimeSpan.FromSeconds(3f); // should not be hardcoded
        var scale = ent.Comp2.Scale;
        var lenFloat = (float) length.TotalSeconds;

        // TODO use like actual easings here
        return new Animation
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(scale * 1.4f, 0.0f),
                        new AnimationTrackProperty.KeyFrame(scale, lenFloat * 0.75f)
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                }
            }
        };
    }
}
