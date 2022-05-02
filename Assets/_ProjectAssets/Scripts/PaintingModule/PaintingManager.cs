using Anura.ConfigurationModule.Managers;
using Anura.Models;
using Anura.Templates.MonoSingleton;
using DTerrain;
using UnityEngine;

public class PaintingManager : MonoSingleton<PaintingManager>
{

    [SerializeField] private BasicPaintableLayer primaryLayer;
    [SerializeField] private BasicPaintableLayer secondaryLayer;

    private ShapeConfig currentShape;

    public void RandomShape()
    {
        currentShape = ConfigurationManager.Instance.Shapes.GetRandomShape();
        Debug.Log(currentShape.name);
    }

    public ShapeConfig GetCurrentShape()
    {
        return currentShape;
    }

    public void Destroy(Vector3 hitPoint)
    {
        primaryLayer?.Paint(new PaintingParameters()
        {
            Color = Color.clear,
            Position = new Vector2Int((int)(hitPoint.x * primaryLayer.PPU) - currentShape.GetSize(), (int)(hitPoint.y * primaryLayer.PPU) - currentShape.GetSize()),
            Shape = currentShape.shape,
            PaintingMode = PaintingMode.REPLACE_COLOR,
            DestructionMode = DestructionMode.DESTROY
        });

        secondaryLayer?.Paint(new PaintingParameters()
        {
            Color = Color.clear,
            Position = new Vector2Int((int)(hitPoint.x * secondaryLayer.PPU) - currentShape.GetSize(), (int)(hitPoint.y * secondaryLayer.PPU) - currentShape.GetSize()),
            Shape = currentShape.shape,
            PaintingMode = PaintingMode.REPLACE_COLOR,
            DestructionMode = DestructionMode.NONE
        });

    }

    public void Build(Vector3 hitPoint)
    {
        primaryLayer?.Paint(new PaintingParameters()
        {
            Color = Color.black,
            Position = new Vector2Int((int)(hitPoint.x * primaryLayer.PPU) - currentShape.GetSize(), (int)(hitPoint.y * primaryLayer.PPU) - currentShape.GetSize()),
            Shape = currentShape.shape,
            PaintingMode = PaintingMode.NONE,
            DestructionMode = DestructionMode.BUILD
        });

        secondaryLayer?.Paint(new PaintingParameters()
        {
            Color = Color.black,
            Position = new Vector2Int((int)(hitPoint.x * secondaryLayer.PPU) - currentShape.GetSize(), (int)(hitPoint.y * secondaryLayer.PPU) - currentShape.GetSize()),
            Shape = currentShape.shape,
            PaintingMode = PaintingMode.REPLACE_COLOR,
            DestructionMode = DestructionMode.BUILD
        });
    }
}
