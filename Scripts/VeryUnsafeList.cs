// src*: https://gist.github.com/andrew-raphael-lukasik/09c8a9c29bb5548ea65273653474f8f1
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Segments
{
	public unsafe struct VeryUnsafeList <T> : System.IDisposable where T : unmanaged
	{

		public T* ptr;
		public readonly Allocator allocator;
		public int length;
		public int capacity;
		public int allocatedBytes { get; private set; }

		public VeryUnsafeList ( Allocator allocator )
			: this( initialCapacity:0 , allocator:allocator ) {}
		public VeryUnsafeList ( int initialCapacity , Allocator allocator )
		{
			Assert.IsFalse( initialCapacity<0 , "invalid capacity" );
			Assert.IsFalse( allocator==Allocator.Invalid , "invalid allocator" );

			this.length = 0;
			this.capacity = initialCapacity;
			this.allocator = allocator;

			this.ptr = null;
			this.allocatedBytes = 0;
			this.Resize( newCapacity:initialCapacity );
		}

		public T this [ int index ]
		{
			get
			{
				if( !AssertIndex(index) ) return default(T);
				return this.ptr[index];
			}
			set
			{
				if( !AssertIndex(index) ) return;
				this.ptr[index] = value;
			}
		}

		public static VeryUnsafeList<T>* Factory ( Allocator allocator ) => Factory( initialCapacity:0 , allocator );
		public static VeryUnsafeList<T>* Factory ( int initialCapacity , Allocator allocator )
		{
			VeryUnsafeList<T>* listPtr = (VeryUnsafeList<T>*) UnsafeUtility.Malloc(
				size:		UnsafeUtility.SizeOf<VeryUnsafeList<T>>() ,
				alignment:	UnsafeUtility.AlignOf<VeryUnsafeList<T>>() ,
				allocator:	allocator
			);
			*listPtr = new VeryUnsafeList<T>( allocator );
			return listPtr;
		}

		public void Add ( T value )
		{
			if( this.capacity==0 )
			{
				this.Resize( 1 );
			}
			if( this.length==this.capacity )
			{
				int old = this.capacity;
				this.Resize( this.capacity * 2 );
			}
			this.ptr[this.length++] = value;
		}

		public void Remove ( T value )
		{
			if( this.length==0 ) return;
			for( int i=0 ; i<this.length ; i++ )
			if( this.ptr[i].Equals(value) )
				this.ptr[i] = this.ptr[--this.length];
		}

		public void RemoveAt ( int index )
		{
			if( !AssertIndex(index) ) return;
			this.ptr[index] = this.ptr[--this.length];
		}

		public void Clear () => this.length = 0;

		public void Resize ( int newCapacity )
		{
			int newAllocatedBytes = UnsafeUtility.SizeOf<T>() * newCapacity;
			T* newPtr = (T*) UnsafeUtility.Malloc( size:newAllocatedBytes , alignment:UnsafeUtility.AlignOf<T>() , allocator:this.allocator );
			if( this.ptr!=null )
			{
				UnsafeUtility.MemCpy( destination:newPtr , source:this.ptr , size:Mathf.Min(this.allocatedBytes,newAllocatedBytes) );
				this.Dispose();
			}
			this.capacity = newCapacity;
			this.ptr = newPtr;
			this.allocatedBytes = newAllocatedBytes;
		}

		public NativeArray<T> ToArray ( Allocator allocator = Allocator.Temp )
		{
			var nativeArray = new NativeArray<T>( this.length , allocator );
			UnsafeUtility.MemCpy( destination:nativeArray.GetUnsafePtr() , source:this.ptr , this.allocatedBytes );
			return nativeArray;
		}
		
		/// <returns>A NativeArray "view" of the list.</returns>
		public NativeArray<T> AsArray ()
		{
			return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>( this.ptr , this.length , Allocator.None );
			// return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>( this.ptr , this.length , this.allocator );
			// @TODO: this array throws null error later on, no idea why
		}

		public void Dispose ()
		{
			if( this.ptr!=null )
			{
				UnsafeUtility.Free( this.ptr , this.allocator );
				this.ptr = null;
				this.allocatedBytes = 0;
			}
		}

		public override string ToString () => $"{{ {nameof(ptr)}:{(long)ptr} , {nameof(allocator)}:{allocator} , {nameof(length)}:{length} , {nameof(capacity)}:{capacity} , {(nameof(allocatedBytes))}:{allocatedBytes} }}";

		public string ToHex ( int elementIndex )
		{
			var bytes = new byte[ this.allocatedBytes ];
			System.Runtime.InteropServices.Marshal.Copy(
				source:			new System.IntPtr( this.ptr + elementIndex ) ,
				destination:	bytes ,
				startIndex:		0 ,
				length:			this.allocatedBytes
			);
			var sb = new System.Text.StringBuilder();
			for( int i=0 ; i<bytes.Length ; i++ )
			{
				byte b = bytes[i];
				string hex = System.Convert.ToString(b,16).ToUpper();
				if( b>15 ) sb.Append($" {hex}");
				else sb.Append($" 0{hex}");
			}
			return sb.ToString();
		}

		bool AssertIndex ( int index )
		{
			if( index<0 || index>=this.length )
			{
				if( index<0 )
				{
					Debug.LogError($"Index is negative: (index) {index} < 0");
					return false;
				}
				else if( index>=this.length )
				{
					Debug.LogError($"Index is larger (or equal) than list length: (index) {index} >= {this.length} (this.length)");
					return false;
				}
				// throw new System.IndexOutOfRangeException();
			}
			return true;
		}

	}
}
