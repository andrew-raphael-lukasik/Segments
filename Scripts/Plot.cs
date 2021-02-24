using Debug = UnityEngine.Debug;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Segments
{
	public static class Plot
	{
		#region ellipses



		/// <summary> Plots a ellipse shape. </summary>
		/// <remarks> Will add list elements if necessary. </remarks>
		public static void Ellipse (
			NativeList<float3x2> segments , ref int index ,
			float rx , float ry ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;
			
			Ellipse( segments.AsArray().Slice(index,numSegments) , rx:rx , ry:ry , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void Ellipse (
			NativeArray<float3x2> segments , ref int index ,
			float rx , float ry ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) return;
			
			Ellipse( segments.Slice(index,numSegments) , rx:rx , ry:ry , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}
		
		/// <inheritdoc/> <remarks> Will throw exception if length < numSegments. </remarks>
		public static void Ellipse (
			NativeSlice<float3x2> segments ,
			float rx , float ry ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			float theta = ( 2f * math.PI ) / (float)numSegments;
			int index = 0;
			for( ; index<numSegments ; )
			{
				float f0 = theta * (float)index;
				float f1 = theta * (float)(index+1);
				float3 v0 = math.mul( rot , ( new float3{ x=math.cos(f0)*rx , y=math.sin(f0)*ry } ) );
				float3 v1 = math.mul( rot , ( new float3{ x=math.cos(f1)*rx , y=math.sin(f1)*ry } ) );
				
				segments[index++] = new float3x2{ c0=pos+v0 , c1=pos+v1 };
			}

			// float a = math.max(rx,ry);
			// float b = math.min(rx,ry);
			// float ecc = math.sqrt( 1f - (b*b)/(a*a) );
			// float c = a * ecc;
			// float3 focus = rx>ry ? new float3{x=c} : new float3{y=c};
			
			// foci( pos + math.mul(rot,focus) );
			// foci( pos + math.mul(rot,-focus) );
		}



		[BurstCompile]
		public struct EllipseJob : IJob
		{
			[WriteOnly] NativeSlice<float3x2> segments;
			float rx, ry;
			float3 pos;
			quaternion rot;
			int numSegments;
			public EllipseJob (
				NativeList<float3x2> segments , ref int index ,
				float rx , float ry ,
				float3 pos , quaternion rot ,
				int numSegments
			)
			{
				int bufferSizeRequired = index + numSegments;
				if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

				this.segments = segments.AsArray().Slice(index,numSegments);
				this.rx = rx;
				this.ry = ry;
				this.pos = pos;
				this.rot = rot;
				this.numSegments = numSegments;
				
				index = bufferSizeRequired;
			}
			public EllipseJob (
				NativeArray<float3x2> segments , ref int index ,
				float rx , float ry ,
				float3 pos , quaternion rot ,
				int numSegments
			)
			{
				int bufferSizeRequired = index + numSegments;
				if( segments.Length<bufferSizeRequired ) Debug.LogError($"Assertion failed: segments.Length:{segments.Length} < bufferSizeRequired:{bufferSizeRequired}");

				this.segments = segments.Slice(index,numSegments);
				this.rx = rx;
				this.ry = ry;
				this.pos = pos;
				this.rot = rot;
				this.numSegments = numSegments;
				
				index = bufferSizeRequired;
			}
			void IJob.Execute ()
				=> Ellipse( segments:segments , rx:rx , ry:ry , pos:pos , rot:rot , numSegments:numSegments );
		}



		/// <summary> Plots a ellipse shape at foci point. </summary>
		/// <remarks> Will add list elements if necessary. </remarks>
		public static void EllipseAtFoci (
			NativeList<float3x2> segments , ref int index ,
			float rx , float ry ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			EllipseAtFoci( segments.AsArray().Slice(index,numSegments) , rx:rx , ry:ry , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void EllipseAtFoci (
			NativeArray<float3x2> segments , ref int index ,
			float rx , float ry ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) return;

			EllipseAtFoci( segments.Slice(index,numSegments) , rx:rx , ry:ry , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < numSegments. </remarks>
		public static void EllipseAtFoci (
			NativeSlice<float3x2> segments ,
			float rx , float ry ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			float a = math.max(rx,ry);
			float b = math.min(rx,ry);
			float ecc = math.sqrt( 1f - (b*b)/(a*a) );
			float c = a * ecc;
			float3 focus = rx>ry ? new float3{x=c} : new float3{y=c};

			float theta = ( 2f * math.PI ) / (float)numSegments;
			int index = 0;
			for( ; index<numSegments ; )
			{
				float f0 = theta * (float)index;
				float f1 = theta * (float)(index+1);
				float3 v0 = math.mul( rot , new float3{ x=math.cos(f0)*rx , y=math.sin(f0)*ry } + focus );
				float3 v1 = math.mul( rot , new float3{ x=math.cos(f1)*rx , y=math.sin(f1)*ry } + focus );

				segments[index++] = new float3x2{ c0=pos+v0 , c1=pos+v1 };
			}
		}



		#endregion
		#region circles



		/// <summary> Plots a circle. </summary>
		/// <remarks> Will add list elements if necessary. </remarks>
		public static void Circle (
			NativeList<float3x2> segments , ref int index ,
			float r , float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			Circle( segments.AsArray().Slice(index,numSegments) , r , pos , rot , numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void Circle (
			NativeArray<float3x2> segments , ref int index ,
			float r , float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) return;

			Circle( segments.Slice(index,numSegments) , r , pos , rot , numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < numSegments. </remarks>
		public static void Circle (
			NativeSlice<float3x2> segments ,
			float r , float3 pos , quaternion rot ,
			int numSegments
		)
		{
			float theta = ( 2f * math.PI ) / (float)numSegments;
			for( int i=0 ; i<numSegments ; i++ )
			{
				float f0 = theta * (float)i;
				float f1 = theta * (float)(i+1);
				float3 v0 = math.mul( rot , ( new float3{ x=math.cos(f0) , y=math.sin(f0) } * r ) );
				float3 v1 = math.mul( rot , ( new float3{ x=math.cos(f1) , y=math.sin(f1) } * r ) );
				segments[i] = new float3x2{ c0 = pos+v0 , c1 = pos+v1 };
			}
		}

		[BurstCompile]
		public struct CircleJob : IJob
		{
			[WriteOnly] NativeSlice<float3x2> segments;
			float r;
			float3 pos;
			quaternion rot;
			int numSegments;
			public CircleJob (
				NativeList<float3x2> segments , ref int index ,
				float r , float3 pos , quaternion rot ,
				int numSegments
			)
			{
				int bufferSizeRequired = index + numSegments;
				if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;
				
				this.segments = segments.AsArray().Slice( index , numSegments );
				this.r = r;
				this.pos = pos;
				this.rot = rot;
				this.numSegments = numSegments;
				
				index = bufferSizeRequired;
			}
			public CircleJob (
				NativeArray<float3x2> segments , ref int index ,
				float r , float3 pos , quaternion rot ,
				int numSegments
			)
			{
				int bufferSizeRequired = index + numSegments;
				if( segments.Length<bufferSizeRequired ) Debug.LogError($"Assertion failed: segments.Length:{segments.Length} < bufferSizeRequired:{bufferSizeRequired}");
				
				this.segments = segments.Slice( index , numSegments );
				this.r = r;
				this.pos = pos;
				this.rot = rot;
				this.numSegments = numSegments;
				
				index = bufferSizeRequired;
			}
			
			void IJob.Execute ()
				=> Circle( segments:segments , r:r , pos:pos , rot:rot , numSegments:numSegments );
		}



		#endregion
		#region lines



		/// <summary> Just fills a single segment value. </summary>
		public static void Line (
			NativeList<float3x2> segments , ref int index ,
			float3 start , float3 end
		)
		{
			int bufferSizeRequired = index + 1;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			segments[index++] = new float3x2{ c0=start , c1=end };
			index = bufferSizeRequired;
		}
		
		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void Line (
			NativeArray<float3x2> segments , ref int index ,
			float3 start , float3 end
		)
		{
			int bufferSizeRequired = index + 1;
			if( segments.Length<bufferSizeRequired ) return;

			segments[index++] = new float3x2{ c0=start , c1=end };
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < 1. </remarks>
		public static void Line ( NativeSlice<float3x2> segments , float3 start , float3 end )
			=> segments[0] = new float3x2{ c0=start , c1=end };



		/// <remarks> Will add list elements if necessary. </remarks>
		public static void DashedLine (
			NativeList<float3x2> segments , ref int index ,
			float3 start , float3 end , int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			DashedLine( segments.AsArray().Slice(index,numSegments) , start:start , end:end , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void DashedLine (
			NativeArray<float3x2> segments , ref int index ,
			float3 start , float3 end , int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) return;

			DashedLine( segments.Slice(index,numSegments) , start:start , end:end , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < numDashes. </remarks>
		public static void DashedLine (
			NativeSlice<float3x2> segments ,
			float3 start , float3 end , int numSegments
		)
		{
			int index = 0;
			int max = math.max( numSegments*2-1 , 0 );
			for( int i=0 ; i<max ; i+=2 )
			{
				segments[index++] = new float3x2{
					c0 = math.lerp( start , end , (float)(i) / (float)max ) ,
					c1 = math.lerp( start , end , (float)(i+1) / (float)max )
				};
			}
		}



		/// <remarks> Will add list elements if necessary. </remarks>
		public static void Arrow (
			NativeList<float3x2> segments , ref int index ,
			float3 v1 , float3 v2
		)
		{
			int bufferSizeRequired = index + 4;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;
			
			Arrow( segments.AsArray().Slice(index,4) , v1:v1 , v2:v2 );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void Arrow (
			NativeArray<float3x2> segments , ref int index ,
			float3 v1 , float3 v2
		)
		{
			int bufferSizeRequired = index + 4;
			if( segments.Length<bufferSizeRequired ) return;
			
			Arrow( segments.Slice(index,4) , v1:v1 , v2:v2 );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < 4. </remarks>
		public static void Arrow (
			NativeSlice<float3x2> segments ,
			float3 v1 , float3 v2
		)
		{
			float d = math.distance( v1 , v2 );
			float3 arrowLen = math.normalize(v1-v2) * d * 0.06f;
			float3 v3 = v2 + math.mul( quaternion.Euler( 0 , 0 , math.PI/14f ) , arrowLen );
			float3 v4 = v2 + math.mul( quaternion.Euler( 0 , 0 , -math.PI/14f ) , arrowLen );

			segments[0] = new float3x2{ c0=v1 , c1=v2 };
			segments[1] = new float3x2{ c0=v2 , c1=v3 };
			segments[2] = new float3x2{ c0=v3 , c1=v4 };
			segments[3] = new float3x2{ c0=v4 , c1=v2 };
		}



		/// <remarks> Will add list elements if necessary. </remarks>
		public static void Arrow (
			NativeList<float3x2> segments , ref int index ,
			float3 v1 , float3 v2 , float3 cameraPos
		)
		{
			int bufferSizeRequired = index + 4;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			Arrow( segments.AsArray().Slice(index,4) , v1:v1 , v2:v2 , cameraPos:cameraPos );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void Arrow (
			NativeArray<float3x2> segments , ref int index ,
			float3 v1 , float3 v2 , float3 cameraPos
		)
		{
			int bufferSizeRequired = index + 4;
			if( segments.Length<bufferSizeRequired ) return;

			Arrow( segments.Slice(index,4) , v1:v1 , v2:v2 , cameraPos:cameraPos );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < 4. </remarks>
		public static void Arrow (
			NativeSlice<float3x2> segments ,
			float3 v1 , float3 v2 , float3 cameraPos
		)
		{
			float3 arrowLen = math.normalize(v1-v2) * math.distance(v1,v2) * 0.06f;
			float3 camAxis = math.normalize( v2 - cameraPos );
			float3 v3 = v2 + math.mul( quaternion.AxisAngle( camAxis , math.PI/14f ) , arrowLen );
			float3 v4 = v2 + math.mul( quaternion.AxisAngle( camAxis , -math.PI/14f ) , arrowLen );
			
			segments[0] = new float3x2{ c0=v1 , c1=v2 };
			segments[1] = new float3x2{ c0=v2 , c1=v3 };
			segments[2] = new float3x2{ c0=v3 , c1=v4 };
			segments[3] = new float3x2{ c0=v4 , c1=v2 };
		}



		#endregion
		#region hyperbolas



		/// <remarks> Will add list elements if necessary. </remarks>
		/// <param name="a"> +-y = ( b * math.sqrt( **a**^2 + x^2 ) ) / **a** </param>
		/// <param name="b"> +-y = ( **b** * math.sqrt( a^2 + x^2 ) ) / a </param>
		public static void HyperbolaAtFoci (
			NativeList<float3x2> segments , ref int index ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			HyperbolaAtFoci( segments.AsArray().Slice(index,numSegments) , a:a , b:b , xrange:xrange , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void HyperbolaAtFoci (
			NativeArray<float3x2> segments , ref int index ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) return;

			HyperbolaAtFoci( segments.Slice(index,numSegments) , a:a , b:b , xrange:xrange , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < numSegments. </remarks>
		public static void HyperbolaAtFoci (
			NativeSlice<float3x2> segments ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			float c = math.sqrt( a*a + b*b );
			float2 vertex = new float2{ y=a };
			float3 focus = new float3{ x=0 , y=c };
			float xmin = vertex.x - xrange;
			float xmax = vertex.x + xrange;
			int index = 0;
			for( ; index<numSegments ; )
			{
				float x0 = math.lerp( xmin , xmax , (float)index / (float)numSegments );
				float x1 = math.lerp( xmin , xmax , (float)(index+1) / (float)numSegments );
				float3 v0 = math.mul( rot , ( new float3{ x=x0 , y=(b*math.sqrt(a*a+x0*x0))/a } - focus ) );
				float3 v1 = math.mul( rot , ( new float3{ x=x1 , y=(b*math.sqrt(a*a+x1*x1))/a } - focus ) );
				segments[index++] = new float3x2{ c0=pos+v0 , c1=pos+v1 };
			}
		}



		/// <remarks> Will add list elements if necessary. </remarks>
		/// <param name="a"> +-y = ( b * math.sqrt( **a**^2 + x^2 ) ) / **a** </param>
		/// <param name="b"> +-y = ( **b** * math.sqrt( a^2 + x^2 ) ) / a </param>
		public static void Hyperbola (
			NativeList<float3x2> segments , ref int index ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			Hyperbola( segments.AsArray().Slice(index,bufferSizeRequired) , a:a , b:b , xrange:xrange , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void Hyperbola (
			NativeArray<float3x2> segments , ref int index ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) return;

			Hyperbola( segments.Slice(index,bufferSizeRequired) , a:a , b:b , xrange:xrange , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < numSegments. </remarks>
		public static void Hyperbola (
			NativeSlice<float3x2> segments ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			float c = math.sqrt( a*a + b*b );
			float2 vertex = new float2{ y=a };
			float3 focus = new float3{ y=c };

			float3 fmirror = new float3{ x=1 , y=-1f , z=1 };
			float xmin = vertex.x - xrange;
			float xmax = vertex.x + xrange;
			int index = 0;
			for( int i=0 ; i<numSegments ; i++ )
			{
				float x0 = math.lerp( xmin , xmax , (float)i / (float)numSegments );
				float x1 = math.lerp( xmin , xmax , (float)(i+1) / (float)numSegments );
				
				float3 p0 = new float3{ x=x0 , y=(b*math.sqrt(a*a+x0*x0))/a };
				float3 p1 = new float3{ x=x1 , y=(b*math.sqrt(a*a+x1*x1))/a };
				float3 v0 = math.mul(rot,p0);
				float3 v1 = math.mul(rot,p1);
				segments[index++] = new float3x2{ c0=pos+v0 , c1=pos+v1 };
			}
		}

		[BurstCompile]
		public struct HyperbolaJob : IJob
		{
			[WriteOnly] NativeSlice<float3x2> segments;
			float a, b, xrange;
			float3 pos;
			quaternion rot;
			int numSegments;
			public HyperbolaJob (
				NativeList<float3x2> segments , ref int index ,
				float a , float b , float xrange ,
				float3 pos , quaternion rot ,
				int numSegments
			)
			{
				int bufferSizeRequired = index + numSegments;
				if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;
				
				this.segments = segments.AsArray().Slice( index , numSegments );
				this.a = a;
				this.b = b;
				this.xrange = xrange;
				this.pos = pos;
				this.rot = rot;
				this.numSegments = numSegments;
				
				index = bufferSizeRequired;
			}
			public HyperbolaJob (
				NativeArray<float3x2> segments , ref int index ,
				float a , float b , float xrange ,
				float3 pos , quaternion rot ,
				int numSegments
			)
			{
				int bufferSizeRequired = index + numSegments;
				if( segments.Length<bufferSizeRequired ) Debug.LogError($"Assertion failed: segments.Length:{segments.Length} < bufferSizeRequired:{bufferSizeRequired}");
				
				this.segments = segments.Slice( index , numSegments );
				this.a = a;
				this.b = b;
				this.xrange = xrange;
				this.pos = pos;
				this.rot = rot;
				this.numSegments = numSegments;
				
				index = bufferSizeRequired;
			}
			void IJob.Execute ()
				=> Hyperbola( segments:segments , a:a , b:b , xrange:xrange , pos:pos , rot:rot , numSegments:numSegments );
		}

		public static void HyperbolaAsymptotes (
			NativeSlice<float3x2> segments ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot
		)
		{
			float Asymptote ( float x ) => (b/a)*x;
			float2 vertex = new float2{ y=a };
			float xmin = vertex.x - xrange;
			float xmax = vertex.x + xrange;
			
			segments[0] = new float3x2{
				c0	= pos + math.mul( rot , new float3{ x=xmin , y=Asymptote(xmin) } ) ,
				c1	= pos + math.mul( rot , new float3{ x=xmax , y=Asymptote(xmax) } )
			};
			segments[1] = new float3x2{
				c0	= pos + math.mul( rot , new float3{ x=xmin , y=-Asymptote(xmin) } ) ,
				c1	= pos + math.mul( rot , new float3{ x=xmax , y=-Asymptote(xmax) } )
			};
		}



		#endregion
		#region parabolas



		/// <remarks> Will add list elements if necessary. </remarks>
		/// <param name="a"> y = **a**xx + bx + c </param>
		/// <param name="b"> y = axx + **b**x + c </param>
		/// <param name="c"> y = axx + bx + **c** </param>
		public static void Parabola (
			NativeList<float3x2> segments , ref int index ,
			float a , float b ,
			float xmin , float xmax ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			Parabola( segments.AsArray().Slice(index,numSegments) , a:a , b:b , xmin:xmin , xmax:xmax , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void Parabola (
			NativeArray<float3x2> segments , ref int index ,
			float a , float b ,
			float xmin , float xmax ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) return;

			Parabola( segments.Slice(index,numSegments) , a:a , b:b , xmin:xmin , xmax:xmax , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < 12. </remarks>
		public static void Parabola (
			NativeSlice<float3x2> segments ,
			float a , float b ,
			float xmin , float xmax ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			const float c = 0;
			// float2 vertex = new float2{ x = -b / (2 * a) , y = ((4 * a * c) - (b * b)) / (4 * a) };
			// float3 focus = new float3{ x = -b / (2 * a) , y = ((4 * a * c) - (b * b) + 1) / (4 * a) };
			for( int index=0 ; index<numSegments ; index++ )
			{
				float x0 = math.lerp( xmin , xmax , (float)index / (float)numSegments );
				float x1 = math.lerp( xmin , xmax , (float)(index+1) / (float)numSegments );
				float3 v0 = math.mul( rot , new float3{ x=x0 , y=a*x0*x0+b*x0+c } );
				float3 v1 = math.mul( rot , new float3{ x=x1 , y=a*x1*x1+b*x1+c } );
				segments[ index ] = new float3x2{ c0=pos+v0 , c1=pos+v1 };
			}
		}

		/// <remarks> Will throw exception if length < 1. </remarks>
		public static void ParabolaDirectrix (
			NativeSlice<float3x2> segments ,
			float a , float b ,
			float xmin , float xmax ,
			float3 pos , quaternion rot
		)
		{
			const float c = 0;
			float directrix_y = c - ((b*b) + 1) * 4 * a;
			segments[0] = new float3x2{
				c0	= pos + math.mul( rot , new float3{ x=xmin , y=directrix_y } ) ,
				c1	= pos + math.mul( rot , new float3{ x=xmax , y=directrix_y } )
			};
		}

		[BurstCompile]
		public struct ParabolaJob : IJob
		{
			[WriteOnly] NativeSlice<float3x2> segments;
			float a, b, xmin, xmax;
			float3 pos;
			quaternion rot;
			int numSegments;
			public ParabolaJob (
				NativeList<float3x2> segments , ref int index ,
				float a , float b ,
				float xmin , float xmax ,
				float3 pos , quaternion rot ,
				int numSegments
			)
			{
				int bufferSizeRequired = index + numSegments;
				if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;
				
				this.segments = segments.AsArray().Slice( index , numSegments );
				this.a = a;
				this.b = b;
				this.xmin = xmin;
				this.xmax = xmax;
				this.pos = pos;
				this.rot = rot;
				this.numSegments = numSegments;
				
				index = bufferSizeRequired;
			}
			public ParabolaJob (
				NativeArray<float3x2> segments , ref int index ,
				float a , float b ,
				float xmin , float xmax ,
				float3 pos , quaternion rot ,
				int numSegments
			)
			{
				int bufferSizeRequired = index + numSegments;
				if( segments.Length<bufferSizeRequired ) Debug.LogError($"Assertion failed: segments.Length:{segments.Length} < bufferSizeRequired:{bufferSizeRequired}");
				
				this.segments = segments.Slice( index , numSegments );
				this.a = a;
				this.b = b;
				this.xmin = xmin;
				this.xmax = xmax;
				this.pos = pos;
				this.rot = rot;
				this.numSegments = numSegments;
				
				index = bufferSizeRequired;
			}
			void IJob.Execute ()
				=> Parabola( segments:segments , a:a , b:b , xmin:xmin , xmax:xmax , pos:pos , rot:rot , numSegments:numSegments );
		}



		/// <remarks> Will add list elements if necessary. </remarks>
		/// <param name="a"> y = **a**xx + bx + c </param>
		/// <param name="b"> y = axx + **b**x + c </param>
		/// <param name="c"> y = axx + bx + **c** </param>
		public static void ParabolaAtFoci (
			NativeList<float3x2> segments , ref int index ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			ParabolaAtFoci( segments.AsArray().Slice(index,numSegments) , a:a , b:b , xrange:xrange , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void ParabolaAtFoci (
			NativeArray<float3x2> segments , ref int index ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			int bufferSizeRequired = index + numSegments;
			if( segments.Length<bufferSizeRequired ) return;

			ParabolaAtFoci( segments.Slice(index,numSegments) , a:a , b:b , xrange:xrange , pos:pos , rot:rot , numSegments:numSegments );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < 12. </remarks>
		public static void ParabolaAtFoci (
			NativeSlice<float3x2> segments ,
			float a , float b , float xrange ,
			float3 pos , quaternion rot ,
			int numSegments
		)
		{
			const float c = 0;
			float2 vertex = new float2{ x = -b / (2 * a) , y = ((4 * a * c) - (b * b)) / (4 * a) };
			float3 focus = new float3{ x = -b / (2 * a) , y = ((4 * a * c) - (b * b) + 1) / (4 * a) };
			float directrix_y = c - ((b*b) + 1) * 4 * a;
			
			float xmin = vertex.x - xrange;
			float xmax = vertex.x + xrange;
			int index = 0;
			for( ; index<numSegments-1 ; )
			{
				float x0 = math.lerp( xmin , xmax , (float)index / (float)numSegments );
				float x1 = math.lerp( xmin , xmax , (float)(index+1) / (float)numSegments );
				float3 v0 = math.mul( rot , ( new float3{ x=x0 , y=a*x0*x0+b*x0+c } - focus ) );
				float3 v1 = math.mul( rot , ( new float3{ x=x1 , y=a*x1*x1+b*x1+c } - focus ) );

				segments[index++] = new float3x2{ c0=pos+v0 , c1=pos+v1 };
			}
			
			segments[index++] = new float3x2{
				c0	= math.mul( rot , new float3{ x=xmin , y=directrix_y } - focus ) + new float3{x=pos.z} ,
				c1	= math.mul( rot , new float3{ x=xmax , y=directrix_y } - focus ) + new float3{x=pos.z}
			};
		}



		#endregion
		#region boxes



		/// <summary> Plots a cube with 12 segments. </summary>
		/// <remarks> Will add list elements if necessary. </remarks>
		public static void Cube ( NativeList<float3x2> segments , ref int index , float a , float3 pos , quaternion rot )
			=> Box( segments , ref index , new float3{x=a,y=a,z=a} , pos , rot );
		
		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void Cube ( NativeArray<float3x2> segments , ref int index , float a , float3 pos , quaternion rot )
			=> Box( segments , ref index , new float3{x=a,y=a,z=a} , pos , rot );
		
		/// <inheritdoc/> <remarks> Will throw exception if length < 12. </remarks>
		public static void Cube ( NativeSlice<float3x2> segments , float a , float3 pos , quaternion rot )
			=> Box( segments , new float3{x=a,y=a,z=a} , pos , rot );



		/// <summary> Plots a box with 12 segments. </summary>
		/// <remarks> Will add list elements if necessary. </remarks>
		public static void Box (
			NativeList<float3x2> segments , ref int index ,
			float3 size , float3 pos , quaternion rot
		)
		{
			int bufferSizeRequired = index + 12;
			if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

			Box( segments.AsArray().Slice(index,12) , size , pos , rot );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will does nothing if array is too short. </remarks>
		public static void Box (
			NativeArray<float3x2> segments , ref int index ,
			float3 size , float3 pos , quaternion rot
		)
		{
			int bufferSizeRequired = index + 12;
			if( segments.Length<bufferSizeRequired ) return;

			Box( segments.Slice(index,12) , size , pos , rot );
			index = bufferSizeRequired;
		}

		/// <inheritdoc/> <remarks> Will throw exception if length < 12. </remarks>
		public static void Box (
			NativeSlice<float3x2> segments ,
			float3 size , float3 pos , quaternion rot
		)
		{
			float3 f = size * 0.5f;
			float3 B0 = math.mul( rot , new float3{ x=f.x , y=-f.y , z=-f.z } );
			float3 B1 = math.mul( rot , new float3{ x=-f.x , y=-f.y , z=-f.z } );
			float3 B2 = math.mul( rot , new float3{ x=-f.x , y=-f.y , z=f.z } );
			float3 B3 = math.mul( rot , new float3{ x=f.x , y=-f.y , z=f.z } );
			float3 T0 = math.mul( rot , new float3{ x=f.x , y=f.y , z=-f.z } );
			float3 T1 = math.mul( rot , new float3{ x=-f.x , y=f.y , z=-f.z } );
			float3 T2 = math.mul( rot , new float3{ x=-f.x , y=f.y , z=f.z } );
			float3 T3 = math.mul( rot , new float3{ x=f.x , y=f.y , z=f.z } );
			
			segments[0] = new float3x2{ c0=pos+B0 , c1=pos+B1 };
			segments[1] = new float3x2{ c0=pos+B1 , c1=pos+B2 };
			segments[2] = new float3x2{ c0=pos+B2 , c1=pos+B3 };
			segments[3] = new float3x2{ c0=pos+B3 , c1=pos+B0 };

			segments[4] = new float3x2{ c0=pos+T0 , c1=pos+T1 };
			segments[5] = new float3x2{ c0=pos+T1 , c1=pos+T2 };
			segments[6] = new float3x2{ c0=pos+T2 , c1=pos+T3 };
			segments[7] = new float3x2{ c0=pos+T3 , c1=pos+T0 };

			segments[8] = new float3x2{ c0=pos+B0 , c1=pos+T0 };
			segments[9] = new float3x2{ c0=pos+B1 , c1=pos+T1 };
			segments[10] = new float3x2{ c0=pos+B2 , c1=pos+T2 };
			segments[11] = new float3x2{ c0=pos+B3 , c1=pos+T3 };
		}

		[BurstCompile]
		public struct BoxJob : IJob
		{
			[WriteOnly] NativeSlice<float3x2> segments;
			float3 size, pos;
			quaternion rot;
			public BoxJob (
				NativeList<float3x2> segments , ref int index ,
				float3 size , float3 pos , quaternion rot
			)
			{
				int bufferSizeRequired = index + 12;
				if( segments.Length<bufferSizeRequired ) segments.Length = bufferSizeRequired;

				this.segments = segments.AsArray().Slice(index,12);
				this.size = size;
				this.pos = pos;
				this.rot = rot;
				
				index = bufferSizeRequired;
			}
			public BoxJob (
				NativeArray<float3x2> segments , ref int index ,
				float3 size , float3 pos , quaternion rot
			)
			{
				int bufferSizeRequired = index + 12;
				if( segments.Length<bufferSizeRequired ) Debug.LogError($"Assertion failed: segments.Length:{segments.Length} < bufferSizeRequired:{bufferSizeRequired}");
				
				this.segments = segments.Slice(index,12);
				this.size = size;
				this.pos = pos;
				this.rot = rot;
				
				index = bufferSizeRequired;
			}
			void IJob.Execute ()
				=> Box( segments , size , pos , rot );
		}



		#endregion
	}
}
