using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrminStaticUtilities.Tools
{
    public static class EulerRotationUtility
    {
        public static float ConvertTo360DegreeAngle(float pDegrees)
        {

            float leftOverDegrees = pDegrees % 360;
            if (leftOverDegrees < 0)
            {
                // Plus because leftOverDegrees is a minus number. - - results in plus.
                leftOverDegrees = 360 + leftOverDegrees;
            }
            return leftOverDegrees;
        }

        public static bool CheckIfBetweenAngles(float pAngle, float pMinAngle, float pMaxAngle)
        {
            if (pMinAngle > pMaxAngle)
            {
                if (pAngle > pMinAngle || pAngle < pMaxAngle) return true;
            }
            else
            {
                if (pAngle > pMinAngle && pAngle < pMaxAngle) return true;
            }
            return false;
        }


        public static bool RotateBackOrForward(float pCurrentRotationAngle, float pTargetRotationAngle)
        {
            float currentThreeSixtyAngle = ConvertTo360DegreeAngle(pCurrentRotationAngle);
            float backwardDistance;
            float forwardDistance;

            // Caculate rotation distances.
            if (pTargetRotationAngle < currentThreeSixtyAngle)
            {
                backwardDistance = currentThreeSixtyAngle - pTargetRotationAngle;
            }
            else
            {
                backwardDistance = currentThreeSixtyAngle + 380 - pTargetRotationAngle;
            }

            if (pTargetRotationAngle > currentThreeSixtyAngle)
            {
                forwardDistance = pTargetRotationAngle - currentThreeSixtyAngle;
            }
            else
            {
                forwardDistance = 380 - currentThreeSixtyAngle + pTargetRotationAngle;
            }

            if (forwardDistance < backwardDistance)
            {
                return true;
            }
            else if (backwardDistance < forwardDistance)
            {
                return false;
            }
            else
            {
                bool randomBoolValue = UnityEngine.Random.Range(0, 2) != 0;
                return randomBoolValue;
            }
        }

        public static Vector3 GetMovementDirectionFromCamera(Vector3 pCameraTransformNegativeMoveDir, Transform pMovingTransform, Vector3 pInput, out float pTargetGroundedRotationAngleRadians, out float pTargetGroundRotationAngleDegrees)
        {
            // Get the local position
            Vector3 fromTransformLookDirection = -(pCameraTransformNegativeMoveDir - pMovingTransform.position).normalized;
            float cameraAngle = Mathf.Atan2(fromTransformLookDirection.x, fromTransformLookDirection.z);
            float inputAngle = Mathf.Atan2(pInput.x, pInput.y);
            float movementAngle = cameraAngle + inputAngle;
            pTargetGroundedRotationAngleRadians = movementAngle;
            pTargetGroundRotationAngleDegrees = EulerRotationUtility.ConvertTo360DegreeAngle(movementAngle * (180 / Mathf.PI));
            Vector3 movementDirection = new Vector3(MathF.Cos(movementAngle), 0, MathF.Sin(movementAngle));
            return movementDirection;
        }

        public static float GetYDegreesToFaceTarget(GameObject pOriginGameObject, GameObject pTargetGameObject)
        {
            Vector3 lookDirectionVector = (pTargetGameObject.transform.position - pOriginGameObject.transform.position);
            float radianAngle = Mathf.Atan2(lookDirectionVector.x, lookDirectionVector.z);
            return  radianAngle * Mathf.Rad2Deg;
        }

        public static float GetZDegreesToFaceTarget(GameObject pOriginGameObject, GameObject pTargetGameObject)
        {
            Vector3 lookDirectionVector = (pTargetGameObject.transform.position - pOriginGameObject.transform.position);
            float radianAngle = Mathf.Atan2(lookDirectionVector.y, lookDirectionVector.x);
            return radianAngle * Mathf.Rad2Deg;
        }

        public static float GetXDegreesToFaceTarget(GameObject pOriginGameObject, GameObject pTargetGameObject)
        {
            Vector3 lookDirectionVector = (pTargetGameObject.transform.position - pOriginGameObject.transform.position);
            float radianAngle = Mathf.Atan2(lookDirectionVector.y, lookDirectionVector.z);
            return -radianAngle * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Note: You can also use this with X and Z of course
        /// Note: fill in in the correct order. For Y axis this is new Vector2(x, z) as input and for out put new Vector2(output.x, original.y, output.y)
        /// </summary>
        /// <param name="pDirectionVector"></param>
        /// <returns></returns>
        public static float ConvertNormalizedVectorToDegrees(Vector2 pDirectionVector, out float pAngleInRadians)
        {
            if (pDirectionVector == null) { Debug.LogError("ConvertVectorToDegrees ERROR: Vector was null."); }
            pDirectionVector.Normalize();
            pAngleInRadians = Mathf.Atan2(pDirectionVector.y, pDirectionVector.x);
            float angleInDegrees = pAngleInRadians * Mathf.Rad2Deg;
            return angleInDegrees;
        }
        /// <summary>
        /// Note: These functions go with the idea that 1.0 is up/forward/0 degrees. So if you use unities z axis as forward it has to be entered in the x input
        /// </summary>
        /// <param name="pAngleInDegrees"></param>
        /// <param name="pAngleInRadians"></param>
        /// <returns></returns>
        public static Vector2 ConvertDegreesToVectorWithMagnitude(float orignalMagnitude, float pAngleInDegrees, out float pAngleInRadians)
        {
            pAngleInRadians = pAngleInDegrees * Mathf.Deg2Rad;
            Vector2 directionVector = new Vector2(orignalMagnitude *Mathf.Cos(pAngleInRadians), orignalMagnitude * Mathf.Sin(pAngleInRadians));
            return directionVector;
        }

        public static Vector3 RotateVector3AroundYWithDegrees(Vector3 pVector3Input, float pDegreesToRotate)
        {
            Vector2 vector2Input = new Vector2(pVector3Input.x, pVector3Input.z);
            float orignalMagnitude = vector2Input.magnitude;
            vector2Input.Normalize();
            float degrees = ConvertNormalizedVectorToDegrees(new Vector2(vector2Input.x, vector2Input.y), out float radians);
            float degreesAfterRotation = ConvertTo360DegreeAngle(degrees + pDegreesToRotate);
            Vector2 rotatedVector = ConvertDegreesToVectorWithMagnitude(orignalMagnitude, degreesAfterRotation, out float pRotatedAngleInRadians);
            Vector3 outputVector = new Vector3(rotatedVector.x, pVector3Input.y, rotatedVector.y);
            return outputVector;
        }

        // By checking with Deepseek I realized my mistake. I wasn't normalizing my vector to rotate before converting to vector and I wasn't using the magnitude when converting back.
        public static Vector3 RotateVector3AroundYWithDegreesFromDeepseek(Vector3 pVector3Input, float pDegreesToRotate)
        {
            // Get the current angle from the X-Z plane
            float currentAngleInDegrees = ConvertNormalizedVectorToDegrees(new Vector2(pVector3Input.x, pVector3Input.z), out float radians);
            //Mathf.Atan2(pVector3Input.z, pVector3Input.x) * Mathf.Rad2Deg
            // Add the rotation amount
            float newAngleInDegrees = currentAngleInDegrees + pDegreesToRotate;

            // Convert back to radians
            float newAngleInRadians = newAngleInDegrees * Mathf.Deg2Rad;

            // Calculate the new X and Z components
            float magnitude = new Vector2(pVector3Input.x, pVector3Input.z).magnitude;
            float newX = magnitude * Mathf.Cos(newAngleInRadians);
            float newZ = magnitude * Mathf.Sin(newAngleInRadians);

            // Return the rotated vector (Y component unchanged)
            return new Vector3(newX, pVector3Input.y, newZ);
        }


        // Got these methods from Deepseek 18-6-2026. They give direction for projectiles to fire from a character over 360 degrees or like a shotgun with the GetDirectionsWithAngleDegrees method. 
        public static List<Vector3> GetThreeSixtyDirections(int pAmount)
        {
            List<Vector3> directions = new List<Vector3>();

            if (pAmount <= 0) return directions;

            float angleStep = 360f / pAmount;

            for (int i = 0; i < pAmount; i++)
            {
                float currentAngle = angleStep * i;
                float radians = currentAngle * Mathf.Deg2Rad;

                // X and Z form the circle, Y stays 0
                float x = Mathf.Sin(radians);
                float z = Mathf.Cos(radians);

                Vector3 dir = new Vector3(x, 0f, z).normalized;
                directions.Add(dir);
            }

            return directions;
        }

        public static List<Vector3> GetDirectionsWithAngleDegrees(int pAmount, float pAngleDegrees = 360f)
        {
            List<Vector3> directions = new List<Vector3>();

            if (pAmount <= 0) return directions;

            // Clamp angle to valid range (0-360)
            float constrainedAngle = Mathf.Clamp(pAngleDegrees, 0f, 360f);

            // If angle is 0, return just the forward direction
            if (constrainedAngle == 0f)
            {
                directions.Add(Vector3.forward);
                return directions;
            }

            // Calculate starting angle to center the spread around forward
            float startAngle = -constrainedAngle / 2f;
            float angleStep = constrainedAngle / (pAmount - 1); // -1 so we include both ends

            for (int i = 0; i < pAmount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                float radians = currentAngle * Mathf.Deg2Rad;

                // Rotate around Y axis from forward direction
                float x = Mathf.Sin(radians);
                float z = Mathf.Cos(radians);

                Vector3 dir = new Vector3(x, 0f, z).normalized;
                directions.Add(dir);
            }

            return directions;
        }
    }
}