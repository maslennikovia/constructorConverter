from OCC.Extend.DataExchange import read_step
from OCC.Core.TopExp import TopExp_Explorer
from OCC.Core.TopAbs import TopAbs_FACE, TopAbs_EDGE, TopAbs_VERTEX
from OCC.Core.BRep import BRep_Tool
from OCC.Core.TopoDS import topods_Face, topods_Edge, topods_Vertex

def analyze_step_file(filename):
    # Чтение STEP файла
    shape = read_step(filename)
    
    # Анализ геометрии
    faces = []
    edges = []
    vertices = []
    
    # Извлечение граней
    exp_face = TopExp_Explorer(shape, TopAbs_FACE)
    while exp_face.More():
        face = topods_Face(exp_face.Current())
        faces.append(face)
        exp_face.Next()
    
    # Извлечение ребер
    exp_edge = TopExp_Explorer(shape, TopAbs_EDGE)
    while exp_edge.More():
        edge = topods_Edge(exp_edge.Current())
        edges.append(edge)
        exp_edge.Next()
    
    # Извлечение вершин
    exp_vertex = TopExp_Explorer(shape, TopAbs_VERTEX)
    while exp_vertex.More():
        vertex = topods_Vertex(exp_vertex.Current())
        vertices.append(vertex)
        exp_vertex.Next()
    
    print(f"Грани: {len(faces)}")
    print(f"Ребра: {len(edges)}")
    print(f"Вершины: {len(vertices)}")
    
    return shape, faces, edges, vertices