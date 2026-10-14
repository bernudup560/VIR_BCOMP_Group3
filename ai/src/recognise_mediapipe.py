import json
import math
from pathlib import Path
from collections import defaultdict

DATA_DIR = Path("ai/data/processed/mediapipe")


def load_recording(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def average_pose(recording):
    frames = recording["frames"]

    if not frames:
        raise ValueError("Recording has no usable frames")

    totals = [[0.0, 0.0, 0.0] for _ in range(21)]

    for frame in frames:
        for i, landmark in enumerate(frame["landmarks"]):
            totals[i][0] += landmark[0]
            totals[i][1] += landmark[1]
            totals[i][2] += landmark[2]

    count = len(frames)

    return [
        [
            totals[i][0] / count,
            totals[i][1] / count,
            totals[i][2] / count
        ]
        for i in range(21)
    ]


def average_template(poses):
    totals = [[0.0, 0.0, 0.0] for _ in range(21)]

    for pose in poses:
        for i, point in enumerate(pose):
            totals[i][0] += point[0]
            totals[i][1] += point[1]
            totals[i][2] += point[2]

    count = len(poses)

    return [
        [
            totals[i][0] / count,
            totals[i][1] / count,
            totals[i][2] / count
        ]
        for i in range(21)
    ]


def pose_distance(a, b):
    total = 0.0

    for p1, p2 in zip(a, b):
        dx = p1[0] - p2[0]
        dy = p1[1] - p2[1]
        dz = p1[2] - p2[2]

        total += dx * dx + dy * dy + dz * dz

    return math.sqrt(total / len(a))


training_poses = defaultdict(list)
unknown_recordings = []

for path in DATA_DIR.glob("*.json"):
    recording = load_recording(path)
    label = str(recording["label"]).strip()

    if label.lower() == "unknown":
        unknown_recordings.append((path, recording))
        continue

    training_poses[label].append(average_pose(recording))


templates = {}

for label, poses in training_poses.items():
    templates[label] = average_template(poses)


print("Training samples:")

for label in sorted(training_poses):
    print(f"  Gesture {label}: {len(training_poses[label])} recordings")


if not unknown_recordings:
    print()
    print("No Unknown recordings found.")
    raise SystemExit


unknown_recordings.sort(
    key=lambda item: item[0].stat().st_mtime
)


for unknown_path, unknown_recording in unknown_recordings:

    unknown_pose = average_pose(unknown_recording)

    results = []

    for label, template_pose in templates.items():
        distance = pose_distance(
            unknown_pose,
            template_pose
        )

        results.append((label, distance))

    results.sort(key=lambda x: x[1])

    print()
    print(f"Testing: {unknown_path.name}")
    print()

    print("Distances:")

    for label, distance in results:
        print(
            f"  Gesture {label}: "
            f"{distance:.4f}"
        )

    predicted_label, best_distance = results[0]

    print()
    print(
        f"PREDICTION: Gesture "
        f"{predicted_label}"
    )

    print(
        f"Distance: "
        f"{best_distance:.4f}"
    )

    print("-" * 40)
