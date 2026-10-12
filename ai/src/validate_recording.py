import json
import sys
from pathlib import Path


def percentage(value, total):
    if total == 0:
        return 0.0
    return (value / total) * 100.0


def main():
    if len(sys.argv) != 2:
        print("Usage:")
        print("  python validate_recording.py <recording.json>")
        sys.exit(1)

    path = Path(sys.argv[1])

    if not path.exists():
        print(f"ERROR: File not found: {path}")
        sys.exit(1)

    with path.open("r", encoding="utf-8") as file:
        data = json.load(file)

    label = data.get("label", "UNKNOWN")
    signer = data.get("signerId", "UNKNOWN")
    source = data.get("dataSource", "UNKNOWN")
    frames = data.get("frames", [])

    total_frames = len(frames)

    left_tracked = 0
    right_tracked = 0
    joint_names = set()

    for frame in frames:
        left = frame.get("leftHand", {})
        right = frame.get("rightHand", {})

        if left.get("isTracked", False):
            left_tracked += 1

        if right.get("isTracked", False):
            right_tracked += 1

        for hand in (left, right):
            for joint in hand.get("joints", []):
                name = joint.get("jointName")
                if name:
                    joint_names.add(name)

    print()
    print("=== Hand Recording Validation ===")
    print(f"File: {path.name}")
    print(f"Label: {label}")
    print(f"Signer: {signer}")
    print(f"Source: {source}")
    print(f"Frames: {total_frames}")
    print()

    print(
        f"Left hand tracked: "
        f"{left_tracked}/{total_frames} "
        f"({percentage(left_tracked, total_frames):.1f}%)"
    )

    print(
        f"Right hand tracked: "
        f"{right_tracked}/{total_frames} "
        f"({percentage(right_tracked, total_frames):.1f}%)"
    )

    print()
    print(f"Unique joint names: {len(joint_names)}")

    if joint_names:
        print("Example joints:")
        for name in sorted(joint_names)[:10]:
            print(f"  - {name}")

    print()
    print("=== Result ===")

    if source == "MockHandDataSource":
        print("MOCK RECORDING")
        print("This recording is suitable for development testing,")
        print("but must NOT be used as real Quest training data.")

    elif source == "XRHandDataReader":
        if total_frames == 0:
            print("INVALID: No frames were recorded.")

        elif left_tracked == 0 and right_tracked == 0:
            print("INVALID: XR recording contains no tracked hands.")

        elif any(name.startswith("MockJoint_") for name in joint_names):
            print("INVALID: Mock joints found inside a live XR recording.")

        elif len(joint_names) == 0:
            print("INVALID: Hands were reported as tracked but no joints exist.")

        else:
            print("VALID LIVE XR RECORDING")
            print("Real tracked hand-joint data appears to be present.")

    else:
        print(f"WARNING: Unknown data source '{source}'.")


if __name__ == "__main__":
    main()
