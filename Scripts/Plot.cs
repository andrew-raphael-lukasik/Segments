using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

namespace EcsLineRenderer
{
	public static class Plot
	{
		

		public static void Ellipse
		(
			EntityManager command , Entity[] entities , ref int index ,
			float rx ,
			float ry ,
			float3 pos ,
			quaternion rot ,
			int numSegments = 128
		)
		{
			float theta = ( 2f * math.PI ) / (float)numSegments;
			for( int i=0 ; i<numSegments ; i++ )
			{
				float f0 = theta * (float)i;
				float f1 = theta * (float)(i+1);
				float3 v0 = math.mul( rot , ( new float3{ x=math.cos(f0)*rx , y=math.sin(f0)*ry } ) );
				float3 v1 = math.mul( rot , ( new float3{ x=math.cos(f1)*rx , y=math.sin(f1)*ry } ) );
				command.SetComponentData( entities[index++] , new Segment { start=pos+v0 , end=pos+v1 } );
			}

			float a = math.max(rx,ry);
			float b = math.min(rx,ry);
			float e = math.sqrt( 1f - (b*b)/(a*a) );
			float c = a * e;
			float3 focus = rx>ry ? new float3{x=c} : new float3{y=c};
			
			// foci( pos + math.mul(rot,focus) );
			// foci( pos + math.mul(rot,-focus) );
		}

		public static void EllipseAtFoci
		(
			EntityManager command , Entity[] entities , ref int index ,
			float rx ,
			float ry ,
			float3 pos ,
			quaternion rot ,
			int numSegments = 128
		)
		{
			float a = math.max(rx,ry);
			float b = math.min(rx,ry);
			float e = math.sqrt( 1f - (b*b)/(a*a) );
			float c = a * e;
			float3 focus = rx>ry ? new float3{x=c} : new float3{y=c};

			float theta = ( 2f * math.PI ) / (float)numSegments;
			for( int i=0 ; i<numSegments ; i++ )
			{
				float f0 = theta * (float)i;
				float f1 = theta * (float)(i+1);
				float3 v0 = math.mul( rot , ( new float3{ x=math.cos(f0)*rx , y=math.sin(f0)*ry } + focus ) );
				float3 v1 = math.mul( rot , ( new float3{ x=math.cos(f1)*rx , y=math.sin(f1)*ry } + focus ) );
				command.SetComponentData( entities[index++] , new Segment { start=pos+v0 , end=pos+v1 } );
			}
		}


		public static void Circle
		(
			EntityManager command , Entity[] entities , ref int index ,
			float r ,
			float3 pos ,
			quaternion rot ,
			int numSegments = 128
		)
		{
			float theta = ( 2f * math.PI ) / (float)numSegments;
			for( int i=0 ; i<numSegments ; i++ )
			{
				float f0 = theta * (float)i;
				float f1 = theta * (float)(i+1);
				float3 v0 = math.mul( rot , ( new float3{ x=math.cos(f0) , y=math.sin(f0) } * r ) );
				float3 v1 = math.mul( rot , ( new float3{ x=math.cos(f1) , y=math.sin(f1) } * r ) );
				command.SetComponentData( entities[index++] , new Segment { start=pos+v0 , end=pos+v1 } );
			}
		}


		public static void DashedLine
		(
			EntityManager command , Entity[] entities , ref int index ,
			float3 start , float3 end ,
			int numDashes
		)
		{
			int max = numDashes * 2;
			for( int i=0 ; i<max ; i+=2 )
			{
				var entity = entities[ index++ ];
				command.SetComponentData( entity , new Segment {
					start	= math.lerp( start , end , (float)(i) / (float)max ) ,
					end		= math.lerp( start , end , (float)(i+1) / (float)max )
				});
			}
		}


		public static void Arrow
		(
			EntityManager command , Entity[] entities , ref int index ,
			float2 p1 ,
			float2 p2
		)
		{
			float d = math.distance( p1 , p2 );
			float3 v1 = new float3{ x=p1.x , y=p1.y };
			float3 v2 = new float3{ x=p2.x , y=p2.y };
			float3 arrowLen = math.normalize(v1-v2) * d * 0.06f;
			float3 v3 = v2 + math.mul( quaternion.Euler( 0 , 0 , math.PI/14f ) , arrowLen );
			float3 v4 = v2 + math.mul( quaternion.Euler( 0 , 0 , -math.PI/14f ) , arrowLen );
			
			command.SetComponentData( entities[index++] , new Segment { start=v1 , end=v2 } );
			command.SetComponentData( entities[index++] , new Segment { start=v2 , end=v3 } );
			command.SetComponentData( entities[index++] , new Segment { start=v3 , end=v4 } );
			command.SetComponentData( entities[index++] , new Segment { start=v4 , end=v2 } );
		}
		public static void Arrow
		(
			EntityManager command , Entity[] entities , ref int index ,
			float3 v1 ,
			float3 v2 ,
			float3 cameraPos
		)
		{
			float3 arrowLen = math.normalize(v1-v2) * math.distance(v1,v2) * 0.06f;
			float3 camAxis = math.normalize( v2 - cameraPos );
			float3 v3 = v2 + math.mul( quaternion.AxisAngle( camAxis , math.PI/14f ) , arrowLen );
			float3 v4 = v2 + math.mul( quaternion.AxisAngle( camAxis , -math.PI/14f ) , arrowLen );
			
			command.SetComponentData( entities[index++] , new Segment { start=v1 , end=v2 } );
			command.SetComponentData( entities[index++] , new Segment { start=v2 , end=v3 } );
			command.SetComponentData( entities[index++] , new Segment { start=v3 , end=v4 } );
			command.SetComponentData( entities[index++] , new Segment { start=v4 , end=v2 } );
		}


		/// <param name="a"> +-y = ( b * math.sqrt( **a**^2 + x^2 ) ) / **a** </param>
		/// <param name="b"> +-y = ( **b** * math.sqrt( a^2 + x^2 ) ) / a </param>
		public static void HyperbolaAtFoci
		(
			EntityManager command , Entity[] entities , ref int index ,
			float a , float b ,
			float xrange ,
			float3 pos ,
			quaternion rot ,
			int numSegments = 128
		)
		{
			float c = math.sqrt( a*a + b*b );
			float2 vertex = new float2{ y=a };
			float3 focus = new float3{ x=0 , y=c };
			float xmin = vertex.x - xrange;
			float xmax = vertex.x + xrange;
			for( int i=0 ; i<numSegments ; i++ )
			{
				float x0 = math.lerp( xmin , xmax , (float)i / (float)numSegments );
				float x1 = math.lerp( xmin , xmax , (float)(i+1) / (float)numSegments );
				float3 v0 = math.mul( rot , ( new float3{ x=x0 , y=(b*math.sqrt(a*a+x0*x0))/a } - focus ) );
				float3 v1 = math.mul( rot , ( new float3{ x=x1 , y=(b*math.sqrt(a*a+x1*x1))/a } - focus ) );
				command.SetComponentData( entities[index++] , new Segment { start=pos+v0 , end=pos+v1 } );
			}
		}

		/// <param name="a"> +-y = ( b * math.sqrt( **a**^2 + x^2 ) ) / **a** </param>
		/// <param name="b"> +-y = ( **b** * math.sqrt( a^2 + x^2 ) ) / a </param>
		public static void Hyperbola
		(
			EntityManager command , Entity[] entities , ref int index ,
			float a , float b ,
			float xrange ,
			float3 pos ,
			quaternion rot ,
			int numSegments = 128
		)
		{
			float Asymptote ( float x ) => (b/a)*x;
			float c = math.sqrt( a*a + b*b );
			float2 vertex = new float2{ y=a };
			float3 focus = new float3{ y=c };

			float3 fmirror = new float3{ x=1 , y=-1f , z=1 };
			float xmin = vertex.x - xrange;
			float xmax = vertex.x + xrange;
			for( int i=0 ; i<numSegments ; i++ )
			{
				float x0 = math.lerp( xmin , xmax , (float)i / (float)numSegments );
				float x1 = math.lerp( xmin , xmax , (float)(i+1) / (float)numSegments );
				
				float3 p0 = new float3{ x=x0 , y=(b*math.sqrt(a*a+x0*x0))/a };
				float3 p1 = new float3{ x=x1 , y=(b*math.sqrt(a*a+x1*x1))/a };

				float3 v0 = math.mul(rot,p0);
				float3 v1 = math.mul(rot,p1);
				command.SetComponentData( entities[index++] , new Segment { start=pos+v0 , end=pos+v1 } );

				float3 v0b = math.mul( rot , p0*fmirror );
				float3 v1b = math.mul( rot , p1*fmirror );
				command.SetComponentData( entities[index++] , new Segment { start=pos+v0b , end=pos+v1b } );
			}
			
			command.SetComponentData( entities[index++] , new Segment {
				start	= pos + math.mul( rot , new float3{ x=xmin , y=Asymptote(xmin) } ) ,
				end		= pos + math.mul( rot , new float3{ x=xmax , y=Asymptote(xmax) } )
			} );
			command.SetComponentData( entities[index++] , new Segment {
				start	= pos + math.mul( rot , new float3{ x=xmin , y=-Asymptote(xmin) } ) ,
				end		= pos + math.mul( rot , new float3{ x=xmax , y=-Asymptote(xmax) } )
			} );
		}



		/// <param name="a"> y = **a**xx + bx + c </param>
		/// <param name="b"> y = axx + **b**x + c </param>
		/// <param name="c"> y = axx + bx + **c** </param>
		public static void Parabola
		(
			EntityManager command , Entity[] entities , ref int index ,
			float a , float b ,
			float xmin , float xmax ,
			float3 pos ,
			quaternion rot ,
			int numSegments = 128
		)
		{
			const float c = 0;
			float2 vertex = new float2{ x = -b / (2 * a) , y = ((4 * a * c) - (b * b)) / (4 * a) };
			float3 focus = new float3{ x = -b / (2 * a) , y = ((4 * a * c) - (b * b) + 1) / (4 * a) };
			float directrix_y = c - ((b*b) + 1) * 4 * a;

			
			for( int i=0 ; i<numSegments ; i++ )
			{
				float x0 = math.lerp( xmin , xmax , (float)i / (float)numSegments );
				float x1 = math.lerp( xmin , xmax , (float)(i+1) / (float)numSegments );
				float3 v0 = math.mul( rot , new float3{ x=x0 , y=a*x0*x0+b*x0+c } );
				float3 v1 = math.mul( rot , new float3{ x=x1 , y=a*x1*x1+b*x1+c } );
				command.SetComponentData( entities[index++] , new Segment { start=pos+v0 , end=pos+v1 } );
			}
			
			command.SetComponentData( entities[index++] , new Segment {
				start	= pos + math.mul( rot , new float3{ x=xmin , y=directrix_y } ) ,
				end		= pos + math.mul( rot , new float3{ x=xmax , y=directrix_y } )
			} );
		}

		/// <param name="a"> y = **a**xx + bx + c </param>
		/// <param name="b"> y = axx + **b**x + c </param>
		/// <param name="c"> y = axx + bx + **c** </param>
		public static void ParabolaAtFoci
		(
			EntityManager command , Entity[] entities , ref int index ,
			float a , float b ,
			float xrange ,
			float3 pos ,
			quaternion rot ,
			int numSegments = 128
		)
		{
			const float c = 0;
			float2 vertex = new float2{ x = -b / (2 * a) , y = ((4 * a * c) - (b * b)) / (4 * a) };
			float3 focus = new float3{ x = -b / (2 * a) , y = ((4 * a * c) - (b * b) + 1) / (4 * a) };
			float directrix_y = c - ((b*b) + 1) * 4 * a;
			
			float xmin = vertex.x - xrange;
			float xmax = vertex.x + xrange;
			for( int i=0 ; i<numSegments ; i++ )
			{
				float x0 = math.lerp( xmin , xmax , (float)i / (float)numSegments );
				float x1 = math.lerp( xmin , xmax , (float)(i+1) / (float)numSegments );
				float3 v0 = math.mul( rot , ( new float3{ x=x0 , y=a*x0*x0+b*x0+c } - focus ) );
				float3 v1 = math.mul( rot , ( new float3{ x=x1 , y=a*x1*x1+b*x1+c } - focus ) );
				command.SetComponentData( entities[index++] , new Segment { start=pos+v0 , end=pos+v1 } );
			}
			command.SetComponentData( entities[index++] , new Segment {
				start	= math.mul( rot , new float3{ x=xmin , y=directrix_y } - focus ) + new float3{x=pos.z} ,
				end		= math.mul( rot , new float3{ x=xmax , y=directrix_y } - focus ) + new float3{x=pos.z}
			} );
		}


		/// <summary> 12 segments </summary>
		public static void Cube
		(
			EntityManager command , Entity[] entities , ref int index ,
			float a ,
			float3 pos ,
			quaternion rot
		)
		{
			float f = a * 0.5f;
			float3 B0 = math.mul( rot , new float3{ x=f , y=-f , z=-f } );
			float3 B1 = math.mul( rot , new float3{ x=-f , y=-f , z=-f } );
			float3 B2 = math.mul( rot , new float3{ x=-f , y=-f , z=f } );
			float3 B3 = math.mul( rot , new float3{ x=f , y=-f , z=f } );
			float3 T0 = math.mul( rot , new float3{ x=f , y=f , z=-f } );
			float3 T1 = math.mul( rot , new float3{ x=-f , y=f , z=-f } );
			float3 T2 = math.mul( rot , new float3{ x=-f , y=f , z=f } );
			float3 T3 = math.mul( rot , new float3{ x=f , y=f , z=f } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B0 , end=pos+B1 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B1 , end=pos+B2 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B2 , end=pos+B3 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B3 , end=pos+B0 } );

			command.SetComponentData( entities[index++] , new Segment { start=pos+T0 , end=pos+T1 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+T1 , end=pos+T2 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+T2 , end=pos+T3 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+T3 , end=pos+T0 } );

			command.SetComponentData( entities[index++] , new Segment { start=pos+B0 , end=pos+T0 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B1 , end=pos+T1 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B2 , end=pos+T2 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B3 , end=pos+T3 } );
		}


		/// <summary> 12 segments </summary>
		public static void Box
		(
			EntityManager command , Entity[] entities , ref int index ,
			float3 size ,
			float3 pos ,
			quaternion rot
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
			command.SetComponentData( entities[index++] , new Segment { start=pos+B0 , end=pos+B1 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B1 , end=pos+B2 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B2 , end=pos+B3 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B3 , end=pos+B0 } );

			command.SetComponentData( entities[index++] , new Segment { start=pos+T0 , end=pos+T1 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+T1 , end=pos+T2 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+T2 , end=pos+T3 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+T3 , end=pos+T0 } );

			command.SetComponentData( entities[index++] , new Segment { start=pos+B0 , end=pos+T0 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B1 , end=pos+T1 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B2 , end=pos+T2 } );
			command.SetComponentData( entities[index++] , new Segment { start=pos+B3 , end=pos+T3 } );
		}


	}
}
