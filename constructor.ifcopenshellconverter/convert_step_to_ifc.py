# convert_step_geometry_fixed.py
import sys
import os
from datetime import datetime

# Try multiple import approaches for OCC
try:
    # Standard import
    from OCC.Extend.DataExchange import read_step
    from OCC.Core.TopoDS import TopoDS_Shape
    from OCC.Core.BRepTools import breptools_Write
    from OCC.Core.IFSelect import IFSelect_RetDone
    HAS_OCC = True
    OCC_METHOD = "standard"
    print("✓ PythonOCC loaded via standard import")
except ImportError as e:
    try:
        # Alternative import path
        import OCC
        from OCC.Extend.DataExchange import read_step
        HAS_OCC = True
        OCC_METHOD = "alternative"
        print("✓ PythonOCC loaded via alternative import")
    except ImportError as e2:
        HAS_OCC = False
        print("✗ PythonOCC import failed completely")
        print("Error 1:", e)
        print("Error 2:", e2)

try:
    import ifcopenshell
    import ifcopenshell.api
    HAS_IFCOPENSHELL = True
    print("✓ IfcOpenShell loaded successfully")
except ImportError as e:
    HAS_IFCOPENSHELL = False
    print("✗ IfcOpenShell import failed:", e)

def convert_step_with_geometry(step_path, ifc_path, metadata=None):
    """Convert STEP to IFC with geometry preservation"""
    
    if not HAS_OCC:
        print("PythonOCC not available. Diagnostic info:")
        print(f"Python executable: {sys.executable}")
        print(f"Python version: {sys.version}")
        print(f"Current directory: {os.getcwd()}")
        return False
    
    try:
        print(f"Converting STEP with geometry: {step_path}")
        
        # Read STEP file
        shapes = read_step_file(step_path)
        if not shapes:
            print("No shapes found in STEP file")
            return False
        
        print(f"Found {len(shapes)} shapes")
        
        # Create IFC file
        ifc_file = ifcopenshell.file(schema="IFC4")
        setup_ifc_metadata(ifc_file, step_path, metadata)
        
        # Create structure and transfer geometry
        storey = create_ifc_structure(ifc_file, step_path)
        success_count = transfer_shapes_to_ifc(ifc_file, storey, shapes, step_path)
        
        ifc_file.write(ifc_path)
        print(f"Success! Created IFC with {success_count} geometric elements")
        return True
        
    except Exception as e:
        print(f"Conversion error: {e}")
        import traceback
        traceback.print_exc()
        return False

def read_step_file(step_path):
    """Read STEP file using OCC"""
    try:
        print(f"Reading STEP file: {step_path}")
        
        # Method 1: Use read_step function
        try:
            shape = read_step(step_path)
            if shape:
                return [shape]
        except Exception as e:
            print(f"Method 1 failed: {e}")
        
        # Method 2: Use STEPControl_Reader
        try:
            from OCC.STEPControl import STEPControl_Reader
            reader = STEPControl_Reader()
            status = reader.ReadFile(step_path)
            
            if status == IFSelect_RetDone:
                reader.TransferRoots()
                shapes = []
                for i in range(1, reader.NbShapes() + 1):
                    shape = reader.Shape(i)
                    if shape:
                        shapes.append(shape)
                return shapes
        except Exception as e:
            print(f"Method 2 failed: {e}")
        
        return []
        
    except Exception as e:
        print(f"STEP reading failed: {e}")
        return []

def setup_ifc_metadata(ifc_file, step_path, metadata):
    """Setup IFC metadata"""
    current_time = datetime.now().isoformat()
    step_name = os.path.basename(step_path)
    
    ifc_file.wrapped_data.header.file_name.name = f"Geometry from {step_name}"
    ifc_file.wrapped_data.header.file_name.author = ["PythonOCC Converter"]
    ifc_file.wrapped_data.header.file_name.time_stamp = current_time
    ifc_file.wrapped_data.header.file_name.preprocessor_version = "OCC Geometry Converter"

def create_ifc_structure(ifc_file, step_path):
    """Create IFC structure"""
    project = ifcopenshell.api.run("root.create_entity", ifc_file, ifc_class="IfcProject", name="Geometric Model")
    ifcopenshell.api.run("unit.assign_unit", ifc_file)
    
    site = ifcopenshell.api.run("root.create_entity", ifc_file, ifc_class="IfcSite", name="Site")
    building = ifcopenshell.api.run("root.create_entity", ifc_file, ifc_class="IfcBuilding", name="Building")
    storey = ifcopenshell.api.run("root.create_entity", ifc_file, ifc_class="IfcBuildingStorey", name="Level 0")
    
    ifcopenshell.api.run("aggregate.assign_object", ifc_file, product=site, relating_object=project)
    ifcopenshell.api.run("aggregate.assign_object", ifc_file, product=building, relating_object=site)
    ifcopenshell.api.run("aggregate.assign_object", ifc_file, product=storey, relating_object=building)
    
    return storey

def transfer_shapes_to_ifc(ifc_file, container, shapes, step_path):
    """Transfer OCC shapes to IFC"""
    success_count = 0
    
    for i, shape in enumerate(shapes):
        try:
            # Create product
            product_name = f"Shape_{i+1:03d}"
            product = ifcopenshell.api.run("root.create_entity", ifc_file, 
                                         ifc_class="IfcBuildingElementProxy", 
                                         name=product_name)
            product.Description = f"Geometric element from STEP"
            
            # Try to create geometry representation
            if create_occ_geometry(ifc_file, product, shape):
                ifcopenshell.api.run("spatial.assign_container", ifc_file, 
                                   product=product, relating_structure=container)
                success_count += 1
                print(f"Converted shape {i+1}")
            else:
                print(f"Geometry creation failed for shape {i+1}")
                
        except Exception as e:
            print(f"Failed to convert shape {i+1}: {e}")
    
    return success_count

def create_occ_geometry(ifc_file, product, shape):
    """Create IFC geometry from OCC shape"""
    try:
        # For OCC geometry, we need to use IfcOpenShell's OCC support
        settings = ifcopenshell.geom.settings()
        settings.set(settings.USE_PYTHON_OPENCASCADE, True)
        
        # Create shape representation using OCC geometry
        shape_representation = ifcopenshell.geom.create_shape(settings, shape)
        
        # Get the geometry representation
        representation = shape_representation.geometry
        
        # Create proper IFC representation structure
        context = ifc_file.by_type("IfcGeometricRepresentationContext")[0]
        
        shape_rep = ifc_file.createIfcShapeRepresentation(
            context,
            "Body",
            "Brep",  # or "SurfaceModel", "Tessellation" depending on the shape
            [representation]
        )
        
        # Create product definition shape
        product_definition_shape = ifc_file.createIfcProductDefinitionShape(
            Representations=[shape_rep]
        )
        
        product.Representation = product_definition_shape
        return True
        
    except Exception as e:
        print(f"Geometry creation error: {e}")
        return False

def main():
    if len(sys.argv) < 3:
        print("Usage: python convert_step_geometry_fixed.py <input.step> <output.ifc>")
        sys.exit(1)
    
    input_step = sys.argv[1]
    output_ifc = sys.argv[2]
    
    if not os.path.exists(input_step):
        print(f"Input file not found: {input_step}")
        sys.exit(1)
    
    print("=== Starting Conversion ===")
    success = convert_step_with_geometry(input_step, output_ifc)
    
    if success:
        print("✓ Conversion completed successfully!")
        sys.exit(0)
    else:
        print("✗ Conversion failed!")
        sys.exit(1)

if __name__ == "__main__":
    main()