import numpy as np
import math
from stl import mesh

def create_directional_model(filename, num_triangles=10000000):
    """
    方向性を持つ大規模STLモデルを作成します。
    基本的な形状として矢印付きの円錐と、XYZ軸を表す特徴を持ちます。
    
    Parameters:
    -----------
    filename : str
        出力するSTLファイルの名前
    num_triangles : int
        おおよその目標三角形数
    """
    # 必要な解像度を計算
    base_resolution = int(math.sqrt(num_triangles / 10))
    print(f"計算された基本解像度: {base_resolution} (目標三角形数: {num_triangles})")
    print(f"STLファイル生成を開始します: {filename}")
    
    # 頂点とface配列を初期化
    vertices = []
    faces = []
    total_expected_steps = 5  # 主要な処理ステップの数
    current_step = 0
    
    # ---------- 主要な形状（変形した円錐）を作成 ----------
    current_step += 1
    print(f"進捗: [{current_step}/{total_expected_steps}] 主要な円錐形状を生成中...")
    
    cone_height = 15.0
    cone_radius = 8.0
    vertex_offset = 0  # 頂点インデックスのオフセット
    
    # 円錐の頂点を追加
    vertices.append([0, 0, cone_height])  # 円錐の先端
    
    # 円錐の底面の頂点
    resolution = base_resolution
    for i in range(resolution):
        theta = 2 * math.pi * i / resolution
        
        # 基本座標
        x = cone_radius * math.cos(theta)
        y = cone_radius * math.sin(theta)
        z = 0
        
        # ノイズを加えて不規則性を持たせる
        noise_amplitude = 0.5
        noise = noise_amplitude * math.sin(10 * theta)
        x += noise * math.cos(theta)
        y += noise * math.sin(theta)
        
        vertices.append([x, y, z])
    
    # 円錐の面を作成
    for i in range(resolution):
        p1 = 0 + vertex_offset  # 円錐の先端
        p2 = 1 + i + vertex_offset
        p3 = 1 + ((i + 1) % resolution) + vertex_offset
        faces.append([p1, p2, p3])
    
    # 円錐の底面を作成（中心点を追加）
    vertices.append([0, 0, 0])  # 底面の中心点
    center_idx = len(vertices) - 1
    
    for i in range(resolution):
        p1 = center_idx
        p2 = 1 + ((i + 1) % resolution) + vertex_offset
        p3 = 1 + i + vertex_offset
        faces.append([p1, p2, p3])
    
    # 次の形状のための頂点インデックスオフセットを更新
    vertex_offset = len(vertices)
    
    # ---------- X, Y, Z軸を表す突起を追加 ----------
    current_step += 1
    print(f"進捗: [{current_step}/{total_expected_steps}] X, Y, Z軸の突起を追加中...")
    
    # X軸の突起（赤）- 正のX方向に伸びる
    x_length = 12.0
    x_width = 1.0
    x_vertices = [
        [cone_radius, x_width, x_width],  # 基部
        [cone_radius, -x_width, x_width],
        [cone_radius, -x_width, -x_width],
        [cone_radius, x_width, -x_width],
        [cone_radius + x_length, 0, 0],  # 先端
    ]
    
    for v in x_vertices:
        vertices.append(v)
    
    # X軸の突起の面
    x_faces = [
        [0, 1, 4],
        [1, 2, 4],
        [2, 3, 4],
        [3, 0, 4],
        [0, 3, 1],
        [1, 3, 2]
    ]
    
    for face in x_faces:
        faces.append([face[0] + vertex_offset, face[1] + vertex_offset, face[2] + vertex_offset])
    
    # 次の形状のための頂点インデックスオフセットを更新
    vertex_offset = len(vertices)
    
    # Y軸の突起（緑）- 正のY方向に伸びる
    y_length = 12.0
    y_width = 1.0
    y_vertices = [
        [y_width, cone_radius, y_width],  # 基部
        [-y_width, cone_radius, y_width],
        [-y_width, cone_radius, -y_width],
        [y_width, cone_radius, -y_width],
        [0, cone_radius + y_length, 0],  # 先端
    ]
    
    for v in y_vertices:
        vertices.append(v)
    
    # Y軸の突起の面
    y_faces = [
        [0, 1, 4],
        [1, 2, 4],
        [2, 3, 4],
        [3, 0, 4],
        [0, 3, 1],
        [1, 3, 2]
    ]
    
    for face in y_faces:
        faces.append([face[0] + vertex_offset, face[1] + vertex_offset, face[2] + vertex_offset])
    
    # 次の形状のための頂点インデックスオフセットを更新
    vertex_offset = len(vertices)
    
    # Z軸の突起（青）- 正のZ方向に伸びる
    z_length = 12.0
    z_width = 1.0
    z_vertices = [
        [z_width, z_width, cone_height],  # 基部
        [-z_width, z_width, cone_height],
        [-z_width, -z_width, cone_height],
        [z_width, -z_width, cone_height],
        [0, 0, cone_height + z_length],  # 先端
    ]
    
    for v in z_vertices:
        vertices.append(v)
    
    # Z軸の突起の面
    z_faces = [
        [0, 1, 4],
        [1, 2, 4],
        [2, 3, 4],
        [3, 0, 4],
        [0, 3, 1],
        [1, 3, 2]
    ]
    
    for face in z_faces:
        faces.append([face[0] + vertex_offset, face[1] + vertex_offset, face[2] + vertex_offset])
    
    # 次の形状のための頂点インデックスオフセットを更新
    vertex_offset = len(vertices)
    
    # ---------- 矢印の特徴を追加 ----------
    current_step += 1
    print(f"進捗: [{current_step}/{total_expected_steps}] 矢印パターンを追加中...")
    
    # 円錐の側面に矢印パターンを刻む（Z軸方向に沿って上向き）
    arrow_height = cone_height * 0.7
    arrow_width = cone_radius * 0.5
    arrow_depth = 0.3
    
    # 矢印の形状を定義
    arrow_resolution = base_resolution // 10
    for i in range(arrow_resolution):
        # 矢印の位置（cone周囲の角度）
        angle = 2 * math.pi * i / arrow_resolution
        
        # 矢印の基部中心
        center_x = cone_radius * 0.8 * math.cos(angle)
        center_y = cone_radius * 0.8 * math.sin(angle)
        center_z = cone_height * 0.3
        
        # 矢印の形状を高密度の三角形で作成
        detail_res = base_resolution // 20
        for j in range(detail_res):
            for k in range(detail_res):
                # 矢印の底部から先端まで変数
                t1 = j / detail_res
                t2 = (j + 1) / detail_res
                s1 = k / detail_res
                s2 = (k + 1) / detail_res
                
                # 矢印のシェイプ関数（先端に向かって細くなる）
                width_factor1 = 1.0 - t1
                width_factor2 = 1.0 - t2
                
                # 点を計算
                p1 = [
                    center_x + arrow_width * width_factor1 * (s1 - 0.5) * math.cos(angle),
                    center_y + arrow_width * width_factor1 * (s1 - 0.5) * math.sin(angle),
                    center_z + t1 * arrow_height
                ]
                
                p2 = [
                    center_x + arrow_width * width_factor1 * (s2 - 0.5) * math.cos(angle),
                    center_y + arrow_width * width_factor1 * (s2 - 0.5) * math.sin(angle),
                    center_z + t1 * arrow_height
                ]
                
                p3 = [
                    center_x + arrow_width * width_factor2 * (s1 - 0.5) * math.cos(angle),
                    center_y + arrow_width * width_factor2 * (s1 - 0.5) * math.sin(angle),
                    center_z + t2 * arrow_height
                ]
                
                p4 = [
                    center_x + arrow_width * width_factor2 * (s2 - 0.5) * math.cos(angle),
                    center_y + arrow_width * width_factor2 * (s2 - 0.5) * math.sin(angle),
                    center_z + t2 * arrow_height
                ]
                
                # 頂点を追加
                v_idx = len(vertices)
                vertices.append(p1)
                vertices.append(p2)
                vertices.append(p3)
                vertices.append(p4)
                
                # 三角形を追加
                faces.append([v_idx, v_idx + 1, v_idx + 2])
                faces.append([v_idx + 1, v_idx + 3, v_idx + 2])
    
    # ---------- 高密度のテクスチャパターンを表面に追加 ----------
    # ここで残りの三角形を使用して表面に複雑なパターンを追加
    remaining_triangles = num_triangles - len(faces)
    if remaining_triangles > 0:
        current_step += 1
        print(f"進捗: [{current_step}/{total_expected_steps}] テクスチャパターンを追加中 (残り三角形: {remaining_triangles})...")
        
        # 螺旋状のパターンを表面に追加
        spiral_res_theta = int(math.sqrt(remaining_triangles / 10))
        spiral_res_phi = spiral_res_theta * 10
        
        for i in range(spiral_res_phi):
            phi = math.pi * i / spiral_res_phi
            radius_at_phi = cone_radius * (1 - phi / math.pi)
            z_at_phi = cone_height * phi / math.pi
            
            for j in range(spiral_res_theta):
                theta = 2 * math.pi * j / spiral_res_theta + phi * 10  # スパイラル効果
                
                # 基本点
                x1 = radius_at_phi * math.cos(theta)
                y1 = radius_at_phi * math.sin(theta)
                z1 = z_at_phi
                
                # 少しずらした次の点
                theta2 = 2 * math.pi * (j + 1) / spiral_res_theta + phi * 10
                x2 = radius_at_phi * math.cos(theta2)
                y2 = radius_at_phi * math.sin(theta2)
                z2 = z_at_phi
                
                # 次の高さの点
                phi2 = math.pi * (i + 1) / spiral_res_phi
                radius_at_phi2 = cone_radius * (1 - phi2 / math.pi)
                z_at_phi2 = cone_height * phi2 / math.pi
                
                x3 = radius_at_phi2 * math.cos(theta)
                y3 = radius_at_phi2 * math.sin(theta)
                z3 = z_at_phi2
                
                x4 = radius_at_phi2 * math.cos(theta2)
                y4 = radius_at_phi2 * math.sin(theta2)
                z4 = z_at_phi2
                
                # 頂点を追加
                v_idx = len(vertices)
                vertices.append([x1, y1, z1])
                vertices.append([x2, y2, z2])
                vertices.append([x3, y3, z3])
                vertices.append([x4, y4, z4])
                
                # 三角形を追加
                faces.append([v_idx, v_idx + 1, v_idx + 2])
                faces.append([v_idx + 1, v_idx + 3, v_idx + 2])
    
    # NumPy配列に変換
    current_step += 1
    print(f"進捗: [{current_step}/{total_expected_steps}] メッシュデータを処理してファイルに保存中...")
    vertices = np.array(vertices)
    faces = np.array(faces)
    
    # STLメッシュを作成
    stl_mesh = mesh.Mesh(np.zeros(len(faces), dtype=mesh.Mesh.dtype))
    
    # メッシュに頂点データを設定
    for i, face in enumerate(faces):
        for j in range(3):
            stl_mesh.vectors[i][j] = vertices[face[j]]
    
    # ファイルに保存
    stl_mesh.save(filename)
    
    actual_triangles = len(faces)
    print(f"生成された三角形数: {actual_triangles}")
    print(f"ファイルが保存されました: {filename}")
    print("STLファイル生成が完了しました！")
    
    return actual_triangles

def create_complex_oriented_terrain(filename, width=1000, height=1000):
    """
    方向性のある複雑な地形モデルを作成します。
    地形上に方向矢印と境界を追加して方向がわかりやすくします。
    
    Parameters:
    -----------
    filename : str
        出力するSTLファイルの名前
    width, height : int
        地形のグリッド解像度
    """
    print(f"STLファイル生成を開始します: {filename}")
    print(f"地形解像度: {width}x{height}")
    
    # 頂点とface配列を初期化
    vertices = []
    faces = []
    total_expected_steps = 5  # 主要な処理ステップの数
    current_step = 0
    
    # 地形のメッシュグリッドを作成
    current_step += 1
    print(f"進捗: [{current_step}/{total_expected_steps}] 地形のベースメッシュを生成中...")
    
    x = np.linspace(-10, 10, width)
    y = np.linspace(-10, 10, height)
    x_grid, y_grid = np.meshgrid(x, y)
    
    # 複数の周波数の正弦波を使用して複雑な地形を作成
    z_grid = np.zeros((width, height))
    for freq in [1, 2, 3, 5, 8, 13]:
        z_grid += np.sin(x_grid * freq) * np.cos(y_grid * freq) / freq
    
    # 方向性を追加: 勾配を北向き（+Y方向）に大きくする
    z_grid += y_grid * 0.1  # Y方向に向かって高くなる
    
    # 各グリッドポイントの頂点を追加
    for i in range(height):
        for j in range(width):
            vertices.append([x_grid[i, j], y_grid[i, j], z_grid[i, j]])
    
    # 三角形を作成
    for i in range(height - 1):
        for j in range(width - 1):
            p1 = i * width + j
            p2 = p1 + 1
            p3 = (i + 1) * width + j
            p4 = p3 + 1
            
            # 各グリッドセルから2つの三角形を作成
            faces.append([p1, p2, p3])
            faces.append([p2, p4, p3])
    
    # 頂点インデックスのオフセットを保存
    vertex_offset = len(vertices)
    
    # 四隅に方向マーカーを追加
    current_step += 1
    print(f"進捗: [{current_step}/{total_expected_steps}] 方向マーカーを追加中...")
    
    corner_markers = [
        [-10, -10, z_grid[0, 0]],  # 左下 (南西)
        [10, -10, z_grid[0, -1]],   # 右下 (南東)
        [-10, 10, z_grid[-1, 0]],   # 左上 (北西)
        [10, 10, z_grid[-1, -1]]    # 右上 (北東)
    ]
    
    marker_labels = ["SW", "SE", "NW", "NE"]
    
    for idx, (cx, cy, cz) in enumerate(corner_markers):
        # マーカーの高さ
        marker_height = 2.0
        marker_size = 1.0
        
        # マーカー柱の頂点
        marker_base = [
            [cx - marker_size/2, cy - marker_size/2, cz],
            [cx + marker_size/2, cy - marker_size/2, cz],
            [cx + marker_size/2, cy + marker_size/2, cz],
            [cx - marker_size/2, cy + marker_size/2, cz],
            [cx - marker_size/2, cy - marker_size/2, cz + marker_height],
            [cx + marker_size/2, cy - marker_size/2, cz + marker_height],
            [cx + marker_size/2, cy + marker_size/2, cz + marker_height],
            [cx - marker_size/2, cy + marker_size/2, cz + marker_height]
        ]
        
        v_idx = len(vertices)
        for v in marker_base:
            vertices.append(v)
        
        # マーカー柱の面
        marker_faces = [
            # 底面
            [0, 2, 1], [0, 3, 2],
            # 側面
            [0, 1, 5], [0, 5, 4],
            [1, 2, 6], [1, 6, 5],
            [2, 3, 7], [2, 7, 6],
            [3, 0, 4], [3, 4, 7],
            # 上面
            [4, 5, 6], [4, 6, 7]
        ]
        
        for face in marker_faces:
            faces.append([face[0] + v_idx, face[1] + v_idx, face[2] + v_idx])
    
    # 中央に大きな方向矢印を追加 (+Y方向を北として)
    current_step += 1
    print(f"進捗: [{current_step}/{total_expected_steps}] 中央の方向矢印を追加中...")
    
    arrow_resolution = 20
    arrow_radius = 2.0
    arrow_height = 3.0
    arrow_center_x = 0
    arrow_center_y = 0
    arrow_base_z = z_grid[height // 2, width // 2]
    
    # 矢印の底面円の頂点
    center_vertex_idx = len(vertices)
    vertices.append([arrow_center_x, arrow_center_y, arrow_base_z])
    
    # 底面の周囲の頂点
    base_vertices_idx = []
    for i in range(arrow_resolution):
        theta = 2 * math.pi * i / arrow_resolution
        x = arrow_center_x + arrow_radius * math.cos(theta)
        y = arrow_center_y + arrow_radius * math.sin(theta)
        vertices.append([x, y, arrow_base_z])
        base_vertices_idx.append(len(vertices) - 1)
    
    # 矢印の先端
    tip_vertex_idx = len(vertices)
    vertices.append([arrow_center_x, arrow_center_y + arrow_radius * 2, arrow_base_z + arrow_height])
    
    # 底面の三角形
    for i in range(arrow_resolution):
        faces.append([
            center_vertex_idx,
            base_vertices_idx[i],
            base_vertices_idx[(i + 1) % arrow_resolution]
        ])
    
    # 側面の三角形
    for i in range(arrow_resolution):
        faces.append([
            tip_vertex_idx,
            base_vertices_idx[i],
            base_vertices_idx[(i + 1) % arrow_resolution]
        ])
    
    # XY軸のラインマーカーを追加
    current_step += 1
    print(f"進捗: [{current_step}/{total_expected_steps}] 軸ラインマーカーを追加中...")
    
    axis_width = 0.3
    axis_height = 0.5
    
    # X軸（赤）
    x_axis_points = [
        [-9.5, 0, z_grid[height // 2, 0] + axis_height],
        [9.5, 0, z_grid[height // 2, -1] + axis_height]
    ]
    
    # Y軸（緑）
    y_axis_points = [
        [0, -9.5, z_grid[0, width // 2] + axis_height],
        [0, 9.5, z_grid[-1, width // 2] + axis_height]
    ]
    
    # X軸線を作成
    x_start_idx = len(vertices)
    vertices.append([x_axis_points[0][0], x_axis_points[0][1] - axis_width, x_axis_points[0][2]])
    vertices.append([x_axis_points[0][0], x_axis_points[0][1] + axis_width, x_axis_points[0][2]])
    vertices.append([x_axis_points[1][0], x_axis_points[1][1] - axis_width, x_axis_points[1][2]])
    vertices.append([x_axis_points[1][0], x_axis_points[1][1] + axis_width, x_axis_points[1][2]])
    
    faces.append([x_start_idx, x_start_idx + 1, x_start_idx + 2])
    faces.append([x_start_idx + 1, x_start_idx + 3, x_start_idx + 2])
    
    # Y軸線を作成
    y_start_idx = len(vertices)
    vertices.append([y_axis_points[0][0] - axis_width, y_axis_points[0][1], y_axis_points[0][2]])
    vertices.append([y_axis_points[0][0] + axis_width, y_axis_points[0][1], y_axis_points[0][2]])
    vertices.append([y_axis_points[1][0] - axis_width, y_axis_points[1][1], y_axis_points[1][2]])
    vertices.append([y_axis_points[1][0] + axis_width, y_axis_points[1][1], y_axis_points[1][2]])
    
    faces.append([y_start_idx, y_start_idx + 1, y_start_idx + 2])
    faces.append([y_start_idx + 1, y_start_idx + 3, y_start_idx + 2])
    
    # NumPy配列に変換
    current_step += 1
    print(f"進捗: [{current_step}/{total_expected_steps}] メッシュデータを処理してファイルに保存中...")
    vertices = np.array(vertices)
    faces = np.array(faces)
    
    # STLメッシュを作成
    stl_mesh = mesh.Mesh(np.zeros(len(faces), dtype=mesh.Mesh.dtype))
    
    # メッシュに頂点データを設定
    for i, face in enumerate(faces):
        for j in range(3):
            stl_mesh.vectors[i][j] = vertices[face[j]]
    
    # ファイルに保存
    stl_mesh.save(filename)
    
    triangles = len(faces)
    print(f"生成された三角形数: {triangles}")
    print(f"ファイルが保存されました: {filename}")
    print("STLファイル生成が完了しました！")
    
    return triangles

if __name__ == "__main__":
    import argparse
    
    parser = argparse.ArgumentParser(description="""
    方向性のある大規模STLファイルを生成するスクリプト
    
    使用例:
    1. 円錐形状の生成:
       python high_polygon_stl_generator.py --type cone --output cone_model.stl --triangles 5000000
    
    2. 地形モデルの生成:
       python high_polygon_stl_generator.py --type terrain --output terrain_model.stl --width 2000 --height 2000
    
    注意:
    - trianglesパラメータは円錐形状にのみ適用されます
    - width/heightパラメータは地形モデルにのみ適用されます
    - 出力ファイルは.stl形式で保存されます
    """)
    parser.add_argument('--type', choices=['cone', 'terrain'], default='cone',
                        help='生成するジオメトリのタイプ')
    parser.add_argument('--output', default='directional_model.stl',
                        help='出力STLファイル名')
    parser.add_argument('--triangles', type=int, default=10000000,
                        help='目標三角形数（円錐タイプの場合）')
    parser.add_argument('--width', type=int, default=3000,
                        help='地形の幅解像度（地形タイプの場合）')
    parser.add_argument('--height', type=int, default=3000,
                        help='地形の高さ解像度（地形タイプの場合）')
    
    args = parser.parse_args()
    
    if args.type == 'cone':
        create_directional_model(args.output, args.triangles)
    else:
        create_complex_oriented_terrain(args.output, args.width, args.height)