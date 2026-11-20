using UnityEngine;
using Verse;

namespace BeyondOurReach;

// For art drawn with a mirrored mesh when facing west: pawn bodies (vanilla always mirrors the west mesh) and
// mechsuit components (the framework does the same). A _west texture of its own would be mirrored back into an
// east view, so the east texture is used there and the mirror turns it into the west view. Use it only where the
// _west art is the mirror of _east, as with the Helios body and rocket pod.
public class Graphic_MultiMirrorWest : Graphic_Multi
{
    public override Material MatWest => MatEast;

    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo) =>
        GraphicDatabase.Get<Graphic_MultiMirrorWest>(path, newShader, drawSize, newColor, newColorTwo, data, maskPath);
}
