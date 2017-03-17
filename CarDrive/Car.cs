﻿using CarDrive.Exceptions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;

namespace CarDrive
{
    class Car
    {
        private double _angle;
        public delegate void Redraw();
        public event Redraw Render;
        public Point Center { get; set; }
        public List<Polyline> Obstacles { private get; set; }
        public double Left, Forward, Right;
        public double FaceAngle
        {
            get
            {
                return _angle;
            }
            private set
            {
                if (value > 360)
                {
                    _angle = value - 360;
                }
                if (value < 0)
                {
                    _angle = value + 360;
                }
                _angle = value;
            }
        }

        public double FaceRadian => TransferToRadian(FaceAngle);
        public double Radius { get; }

        public Car()
        {
            FaceAngle = 90;
            Radius = 3;
        }

        /// <summary>
        /// Move the car.
        /// </summary>
        /// <param name="degree">The steering wheel degree.</param>
        /// <exception cref="ArgumentException">Degree is larger than 40 degree.</exception>
        public void Move(double degree)
        {
            if (Math.Abs(degree) > 40)
            {
                throw new ArgumentException();
            }
            double x = Center.X + Math.Cos(TransferToRadian(FaceAngle + degree)) + Math.Sin(TransferToRadian(degree)) * Math.Sin(FaceRadian);
            double y = Center.Y + Math.Sin(TransferToRadian(FaceAngle + degree)) - Math.Sin(TransferToRadian(degree)) * Math.Cos(FaceRadian);
            Center = new Point(x, y);
            FaceAngle = FaceAngle - TransferToDegree(Math.Asin(2 * Math.Sin(TransferToRadian(degree)) / (2 * Radius)));

            UpdateSensor();
            Render?.Invoke();
        }

        private void UpdateSensor()
        {
            Left = GetDistanceLeft();
            Forward = GetDistanceForward();
            Right = GetDistanceRight();
        }

        private double TransferToRadian(double degree)
        {
            return degree * Math.PI / 180;
        }

        private double TransferToDegree(double radian)
        {
            return radian * 180 / Math.PI;
        }

        public void Reset()
        {
            Center = new Point(0, 0);
            FaceAngle = 90;
            Render?.Invoke();
        }

        private double GetDistanceLeft()
        {
            return GetDistanceFromDegree(45);
        }

        private double GetDistanceForward()
        {
            return GetDistanceFromDegree(0);
        }

        private double GetDistanceRight()
        {
            return GetDistanceFromDegree(-45);
        }

        private double GetDistanceFromDegree(double degree)
        {
            double distance = Double.MaxValue, bonusRadian = degree * Math.PI / 180;

            Line laser = new Line()
            {
                X1 = Center.X,
                Y1 = Center.Y,
                X2 = Center.X + Math.Cos(FaceRadian + bonusRadian),
                Y2 = Center.Y + Math.Sin(FaceRadian + bonusRadian)
            };

            foreach (Polyline polyLine in Obstacles)
            {
                var points = polyLine.Points;
                for (int i = 0; i < points.Count - 1; ++i)
                {
                    try
                    {
                        Line obstacle = new Line()
                        {
                            X1 = points[i].X,
                            Y1 = points[i].Y,
                            X2 = points[i + 1].X,
                            Y2 = points[i + 1].Y
                        };
                        Point intersectionPoint = Intersect(laser, obstacle);
                        distance = Math.Min(distance, GetTwoPointDistance(Center, intersectionPoint));
                    }
                    catch (NoIntersectException)
                    {
                    }
                }
            }

            return distance;
        }

        /// <summary>
        /// Get the intersection point.
        /// </summary>
        /// <param name="laser">Laser's vector.</param>
        /// <param name="obstacle">Obstacle's data.</param>
        /// <returns>Intersection point</returns>
        /// <exception cref="NoIntersectException">There is no intersection found.</exception>
        private Point Intersect(Line laser, Line obstacle)
        {
            // ax - y = b
            Point intersectPoint = new Point();
            var laserPara = GenerateLinearEquations(laser);
            var obstaclePara = GenerateLinearEquations(obstacle);
            // Ax + By = C
            // delta = A1*B2 - A2*B1
            // 1 is laser, 2 is obstacle
            double delta = laserPara.coefficientX * obstaclePara.coefficientY - obstaclePara.coefficientX * laserPara.coefficientY;
            if (delta == 0)
            {
                throw new NoIntersectException();
            }

            // x = (B2*C1 - B1*C2)/delta;
            // y = (A1*C2 - A2*C1)/delta;
            intersectPoint.X = (obstaclePara.coefficientY * laserPara.constant - laserPara.coefficientY * obstaclePara.constant) / delta;
            intersectPoint.Y = (laserPara.coefficientX * obstaclePara.constant - obstaclePara.coefficientX * laserPara.constant) / delta;

            // Check if point in line segmentation
            double obsMinX = Math.Min(obstacle.X1, obstacle.X2);
            double obsMaxX = Math.Max(obstacle.X1, obstacle.X2);
            double obsMinY = Math.Min(obstacle.Y1, obstacle.Y2);
            double obsMaxY = Math.Max(obstacle.Y1, obstacle.Y2);
            if (intersectPoint.X < obsMinX || intersectPoint.X > obsMaxX || intersectPoint.Y < obsMinY || intersectPoint.Y > obsMaxY)
            {
                throw new NoIntersectException();
            }

            // Check if point in the right way of vector
            if ((intersectPoint.X - laser.X1) * (laser.X2 - laser.X1) < 0)
            {
                throw new NoIntersectException();
            }
            if ((intersectPoint.Y - laser.Y1) * (laser.Y2 - laser.Y1) < 0)
            {
                throw new NoIntersectException();
            }

            return intersectPoint;
        }

        private (double coefficientX, double coefficientY, double constant) GenerateLinearEquations(Line line)
        {
            // ax - y = b
            double cofficientX = (line.X2 - line.X1 == 0) ? 1 : (line.Y2 - line.Y1) / (line.X2 - line.X1);
            double cofficientY = (line.X2 - line.X1 == 0) ? 0 : -1;
            double constant = cofficientX * line.X1 + cofficientY * line.Y1;

            return (cofficientX, cofficientY, constant);
        }

        private double GetTwoPointDistance(Point start, Point end)
        {
            double distance = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));

            return distance;
        }
    }
}
