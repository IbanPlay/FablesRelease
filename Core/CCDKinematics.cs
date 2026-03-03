using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public class CCDKinematicJoint
    {
        public CCDKinematicJoint Parent { get; set; }
        public CCDKinematicJoint Child { get; set; }

        public CCDKinematicsConstraint Constraints = null;

        public bool soundPlayed = false;

        #region Variable setter/getters
        internal bool _needsRotationRecalculation = true;
        internal bool _needsPositionRecalculation = true;
        internal Vector2 _position;
        internal float _cachedAbsoluteRotation;
        internal float _rotation;
        internal float _jointLenght;
        internal CCDKinematicJoint _endEffector;

        public float Rotation {
            get {
                //The first joint (which is just a hinge) doesn't have a rotation
                if (Parent == null)
                    return 0;

                if (!_needsRotationRecalculation)
                    return _cachedAbsoluteRotation;

                _cachedAbsoluteRotation = Parent.Rotation + _rotation;
                _needsRotationRecalculation = false;
                return _cachedAbsoluteRotation;
            }
            set {
                //First joint is just a hinge, you can't change its rotation
                if (Parent == null)
                {
                    //If the first hinge has a child, the rotation change will instead affect the first limb
                    if (Child != null)
                        Child.Rotation = value;
                    return;
                }

                _needsPositionRecalculation = true;
                _needsRotationRecalculation = true;
                RecursivelyClearChildRotationCaches();

                //If unconstrained, just directly set the rotation.
                if (Constraints == null)
                    _rotation = value - Parent.Rotation;
                //Else, we have to do a bit more trickery.
                else
                {
                    float newRotation = value - Parent.Rotation;
                    float newUnwrappedRotation = _rotation + _rotation.AngleBetween(newRotation);

                    float prevRotation = _rotation;
                    _rotation = Constraints.Apply(newUnwrappedRotation);
                }
            }
        }

        public float JointLenght {
            get => _jointLenght;
            set {
                //Changing the joint lenght means that we need to recalculate the cached position of all children

                RecursivelyClearChildPositionCaches();
                _jointLenght = value;
            }
        }

        public Vector2 Position {
            get {
                //If this is the first part of the limb, just give the direct position
                if (Parent == null)
                    return _position;

                //If we're on part of the limb but we don't need to recalculate it, just give the cached position
                if (!_needsPositionRecalculation)
                    return _position;

                //Else, calculate the position from the parent's position, and cache it
                _position = Parent.Position + Rotation.ToRotationVector2() * JointLenght;
                _needsPositionRecalculation = false;
                return _position;
            }

            set {
                //Can only set the position directly if it's the first part of the limb
                if (Parent == null)
                {
                    _position = value;
                    RecursivelyClearChildPositionCaches(); //Reset the cached positions of all the child segments since they will have been moved by moving the first segment
                }
            }
        }

        public CCDKinematicJoint EndEffector {
            get {

                //If no child, this is the end effector
                if (Child == null)
                    return this;

                //If we cached an end effector, return it
                if (_endEffector != null)
                    return _endEffector;

                _endEffector = GetEndEffector();
                return _endEffector;
            }
        }

        public void RecursivelyClearChildPositionCaches()
        {
            if (Child != null)
            {
                Child._needsPositionRecalculation = true;
                Child.RecursivelyClearChildPositionCaches();
            }
        }

        public void RecursivelyClearChildRotationCaches()
        {
            //Changing the rotation means not only changing the rotations of every other child, but also changing their position
            if (Child != null)
            {
                Child._needsPositionRecalculation = true;
                Child._needsRotationRecalculation = true;
                Child.RecursivelyClearChildRotationCaches();
            }
        }

        internal void RecursivelyClearParentsEndEffectors()
        {
            _endEffector = null;
            if (Parent != null)
                Parent.RecursivelyClearParentsEndEffectors();
        }

        public Vector2 SegmentVector => Rotation.ToRotationVector2() * JointLenght;
        #endregion

        #region Constructors
        public CCDKinematicJoint(Vector2 position)
        {
            Position = position;
            _jointLenght = 0;
            _rotation = 0;
            _cachedAbsoluteRotation = 0;
        }

        public CCDKinematicJoint(Vector2 position, CCDKinematicJoint parent)
        {
            parent.Append(this);

            JointLenght = position.Distance(parent.Position);

            float rotation = (position - parent.Position).ToRotation();
            Rotation = rotation;
        }
        #endregion

        #region Adding children
        /// <summary>
        /// Appends a new joint to the end of the limb
        /// </summary>
        public void Append(CCDKinematicJoint newJoint)
        {
            newJoint.Parent = this;
            Child = newJoint;
            Child.RecursivelyClearChildRotationCaches();
            Child.RecursivelyClearParentsEndEffectors();
        }

        /// <summary>
        /// Creates and appends a new joint to the end of the limb at the specified joint position, with the specified constraitns
        /// </summary>
        public void Append(Vector2 newJointPosition, CCDKinematicsConstraint constraints = null)
        {
            CCDKinematicJoint newJoint = new CCDKinematicJoint(newJointPosition, this);
            newJoint.Constraints = constraints;
            Append(newJoint);
        }

        /// <summary>
        /// Adds a new joint to the final segment from the segment chain this joint is part of
        /// </summary>
        public void Extend(CCDKinematicJoint newJoint)
        {
            if (Child != null)
                Child.Extend(newJoint);
            else
                Append(newJoint);
        }

        /// <summary>
        /// Adds a newly created joint to the final segment from the segment chain this joint is part of with the specified position and constraitns
        /// </summary>
        public void Extend(Vector2 newJointPosition, CCDKinematicsConstraint constraints = null)
        {
            if (Child != null)
                Child.Extend(newJointPosition, constraints);
            else
                Append(newJointPosition, constraints);
        }

        /// <summary>
        /// Adds a newly created joint to the final segment from the segment chain this joint is part of, with the specified offset from the end of the chain
        /// </summary>
        public void ExtendInDirection(Vector2 newJointOffset, CCDKinematicsConstraint constraints = null)
        {
            if (Child != null)
                Child.ExtendInDirection(newJointOffset, constraints);
            else
                Append(Position + newJointOffset, constraints);
        }
        #endregion

        public List<CCDKinematicJoint> GetSubLimb()
        {
            List<CCDKinematicJoint> joints = new List<CCDKinematicJoint>();
            RecursivelyFillSubLimbList(ref joints);
            return joints;
        }

        internal void RecursivelyFillSubLimbList(ref List<CCDKinematicJoint> joints)
        {
            joints.Add(this);
            if (Child != null)
                Child.RecursivelyFillSubLimbList(ref joints);
        }

        public float GetLimbLenght()
        {
            float currentLenght = 0f;
            RecursivelyAddLimbLenght(ref currentLenght);
            return currentLenght;
        }

        internal void RecursivelyAddLimbLenght(ref float lenght)
        {
            lenght += JointLenght;
            if (Child != null)
                Child.RecursivelyAddLimbLenght(ref lenght);
        }

        public float GetDistanceToEndEffector() => EndEffector.Position.Distance(Position);

        public CCDKinematicJoint GetEndEffector()
        {
            if (Child != null)
                return Child.GetEndEffector();
            return this;
        }
    }

    public class CCDKinematicsConstraint
    {
        public bool flipConstraintAngles = false;
        internal float _minimumAngle;
        internal float _maximumAngle;

        public float MinimumAngle {
            get => flipConstraintAngles ? -_maximumAngle : _minimumAngle;
            set => _minimumAngle = value;
        }

        public float MaximumAngle {
            get => flipConstraintAngles ? -_minimumAngle : _maximumAngle;
            set => _maximumAngle = value;
        }

        public CCDKinematicsConstraint(float minimumAngle, float maximumAngle, bool flipped = false)
        {
            flipConstraintAngles = false;
            _minimumAngle = minimumAngle;
            _maximumAngle = maximumAngle;
            flipConstraintAngles = flipped;
        }

        public float Apply(float angle) => MathHelper.Clamp(angle, MinimumAngle, MaximumAngle);
    }

    public static class CCDKinematics
    {
        public static void SimulateLimb(CCDKinematicJoint joint, Vector2 target, int iterations)
        {
            List<CCDKinematicJoint> joints = joint.GetSubLimb();
            CCDKinematicJoint endEffector = joints[joints.Count - 1];

            for (int k = 0; k < iterations; k++)
            {
                for (int i = joints.Count - 1; i >= 1; i--)
                {
                    //Get the angle of the previous joint to the target (aka the "hinge" of the current joint)
                    float angleToTarget = joints[i - 1].Position.AngleTo(target);

                    //If we are at the end effector, just rotate it to point towards the target.
                    if (i == joints.Count - 1)
                        joints[i].Rotation = angleToTarget;
                    else
                    {
                        float angleToEndEffector = joints[i - 1].Position.AngleTo(endEffector.Position);
                        float angleDifference = angleToEndEffector.AngleBetween(angleToTarget);
                        //Rotate so that the angle towards the end effector from the joint's hinge points toward the target.
                        joints[i].Rotation += angleDifference;
                    }
                }
            }
        }

        public static CCDKinematicJoint CreateLimb(params Vector2[] points)
        {
            CCDKinematicJoint firstJoint = new CCDKinematicJoint(points[0]);
            CCDKinematicJoint previousJoint = firstJoint;

            for (int i = 1; i < points.Length; i++)
                previousJoint = new CCDKinematicJoint(points[i], previousJoint);

            return firstJoint;
        }
    }
}
