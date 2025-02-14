using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleService : GameService
{
    public enum Type
    {
        DogPetting
    }

    private SerializedDictionary<Type, ParticleSystem> ParticleSystems = new();

    public void PlayAt(Type Type, Vector3 Position)
    {
        ParticleSystem Target = ParticleSystems[Type];
        if (Target == null)
            return;

        Target.gameObject.transform.position = Position;
        Target.gameObject.SetActive(true);
        Target.Play();
    }

    public void Pause(Type Type)
    {
        ParticleSystems[Type].Pause();
    }

    public void Stop(Type Type)
    {
        ParticleSystems[Type].Stop();
    }

    public void PetDog()
    {
        if (!Game.TryGetService(out Selectors Selectors))
            return;

        HexagonVisualization Hex = Selectors.GetSelectedHexagon();
        if (Hex == null)
            return;

        Vector3 Location = HexagonConfig.TileSpaceToWorldSpace(Hex.Location.GlobalTileLocation);
        Location.y += HexagonConfig.TileSize.y;
        PlayAt(Type.DogPetting, Location);
    }

    private void Init()
    {
        foreach (Transform Child in transform)
        {
            // todo: actual mapping
            ParticleSystem Particles = Child.gameObject.GetComponent<ParticleSystem>();
            if (Particles == null)
                continue;

            Particles.Stop();
            ParticleSystems.Add(Type.DogPetting, Particles);
        }
    }

    protected override void ResetInternal()
    {
        
        ParticleSystems.Clear();
    }

    protected override void StartServiceInternal()
    {
        Init();
    }

    protected override void StopServiceInternal(){}
}
