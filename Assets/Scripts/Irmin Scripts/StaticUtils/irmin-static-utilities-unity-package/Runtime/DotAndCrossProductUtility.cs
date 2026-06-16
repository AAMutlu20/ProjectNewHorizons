using UnityEngine;

public static class DotAndCrossProductUtility
{

    // Using the dot product to calculate the distance of a point from a line
    // Dot product projection

    public static float GetDistanceFromLine2DDotProduct(Vector2 pLine, Vector2 pPointVector, out Vector2 pNormal)
    {
        // The normal here is inverted line with one negated. This rotates it 90 degrees
        pNormal = new Vector2(-pLine.y, pLine.x);
        // The distance is calculated with the vector and the normal
        float distance = Vector2.Dot(pPointVector, pNormal);
        return distance;
    }

    // In 2D you swap x and y and make one negative, in 3D you can use the cross product
    // They use the same trick as to get a normal for the dot product. Three dot products. They get the normal
    // Vector3_1 * Vector3_2 = 
    // y1*z2 - z1*y2
    // z1*x2 - x1*z2
    // x1*y2 - y1*x2
    // For questions during examination: A cross product giver you a vector that is perpendicular to another vector. It can be used to calculate a vector pointing away from two other vectors. Vector3.Cross
    // Teacher also said "A vector pointing way from two other vectors"
    // If you have x and z, y is the cross product of those? it seems
    //
    // Length: Length of the on vector * the length of the other vector then sin of that
    //
    // ||a|| ||b|| means the length of vector a * the length of vector b
    public static Vector3 GetNormal3DCrossProduct(Vector3 pVector1, Vector3 pVector2)
    {
        return Vector3.Cross(pVector1, pVector2);
    }
}
