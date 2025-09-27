namespace VMHud.Core.Contracts;

public interface IMatrixController
{
    void SetRoute(int stripIndex, int busIndex, bool enabled);
    void SetStripGain(int stripIndex, float gainDb);
    void SetBusGain(int busIndex, float gainDb);
}
