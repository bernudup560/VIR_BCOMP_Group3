import json
from pathlib import Path
import math

INPUT_DIR = Path("ai/data/raw/mediapipe")
OUTPUT_DIR = Path("ai/data/processed/mediapipe")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


def normalise_hand(joints):
    """
    Normalise MediaPipe hand landmarks:
    1. Wrist (joint 0) becomes the origin.
    2. Scale hand so distance from wrist is independent of camera distance.
    """

    wrist = joints[0]["position"]

    points = []

    for joint in joints:
        p = joint["position"]

        points.append([
            p["x"] - wrist["x"],
            p["y"] - wrist["y"],
            p["z"] - wrist["z"]
        ])

    max_distance = max(
        math.sqrt(x*x + y*y + z*z)
        for x, y, z in points
    )

    if max_distance == 0:
        max_distance = 1.0

    return [
        [x / max_distance, y / max_distance, z / max_distance]
        for x, y, z in points
    ]


for file in INPUT_DIR.glob("*.json"):

    with open(file, "r", encoding="utf-8") as f:
        data = json.load(f)

    processed_frames = []

    for frame in data["frames"]:

        hand = frame["rightHand"]

        if not hand["isTracked"]:
            continue

        joints = hand["joints"]

        if len(joints) != 21:
            continue

        processed_frames.append({
            "timestamp": frame["timestamp"],
            "landmarks": normalise_hand(joints)
        })

    output = {
        "label": str(data["label"]).strip(),
        "signerId": data.get("signerId"),
        "dataSource": data.get("dataSource"),
        "jointCount": 21,
        "frameCount": len(processed_frames),
        "frames": processed_frames
    }

    output_file = OUTPUT_DIR / file.name

    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(output, f, indent=2)

    print(
        f"{file.name}: "
        f"label={output['label']} "
        f"frames={output['frameCount']}"
    )

