# convert_simple.py
import ifcopenshell
import sys
import json
from datetime import datetime

def convert_step_to_ifc_simple(step_path, ifc_path, metadata=None):
    try:
        print("Starting conversion...")
        
        # Load STEP
        step_file = ifcopenshell.open(step_path)
        print("STEP file loaded")
        
        # Create IFC
        ifc_file = ifcopenshell.file(schema="IFC4")
        
        # Basic metadata
        ifc_file.wrapped_data.header.file_name.name = "Converted Model"
        ifc_file.wrapped_data.header.file_name.author = ["Converter"]
        ifc_file.wrapped_data.header.file_name.time_stamp = datetime.now().isoformat()
        
        # Copy entities
        entity_types = ['IfcWall', 'IfcSlab', 'IfcBeam', 'IfcColumn', 'IfcDoor', 'IfcWindow']
        
        for entity_type in entity_types:
            entities = step_file.by_type(entity_type)
            print(f"Copying {len(entities)} {entity_type}")
            
            for entity in entities:
                try:
                    ifc_file.add(entity)
                except Exception as e:
                    print(f"Failed to copy {entity_type}: {e}")
        
        # Save
        ifc_file.write(ifc_path)
        print("Conversion successful!")
        return True
        
    except Exception as e:
        print(f"Error: {e}")
        return False

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python convert_simple.py input.step output.ifc")
        sys.exit(1)
    
    success = convert_step_to_ifc_simple(sys.argv[1], sys.argv[2])
    sys.exit(0 if success else 1)