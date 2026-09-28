# Database Schema

SQLite database holding SASL hand-tracking recordings (OpenXR, one hand, 26 joints).

## Build it

From the `ai/` folder:

    python database/create_db.py

This creates `data/raw/sasl.db` (gitignored). Never commit the `.db` file: it will contain participant data.

## Tables

| Table | Purpose |
|---|---|
| Signs | Reference list of signs (A-Z, 1-10). `IsDynamic` = 1 for signs that need motion (J, Z). |
| HandJoints | The 26 OpenXR joints, IDs 0-25. |
| Participants | Who was recorded: dominant hand, Deaf/hearing/learner, consent reference. |
| Recordings | One take of one sign: participant, hand used, device, correct/incorrect, review status. |
| FrameData | One row per captured frame, with timestamp in ms since the start of the recording. |
| JointCoordinates | Per frame, per joint: position (x, y, z), rotation quaternion, radius, tracked flag. |

## Notes

- Incorrect attempts are stored too (`IsCorrectExample = 0`, with `MistakeType`) so the model can learn to spot mistakes.
- Only `ReviewStatus = 'Approved'` recordings should be used for training.
- Sign definitions (especially which signs are dynamic) must be verified by a Deaf SASL signer or consultant.
- The AI training code should read from this database and export tensors. The exact export format is still to be defined here.
