PRAGMA foreign_keys = ON;

-- ---------- REFERENCE TABLES ----------
CREATE TABLE Signs (
    SignID       INTEGER PRIMARY KEY AUTOINCREMENT,
    SignLabel    TEXT NOT NULL UNIQUE,
    SignCategory TEXT NOT NULL CHECK (SignCategory IN ('Letter','Number','Word')),
    IsDynamic    INTEGER NOT NULL DEFAULT 0 CHECK (IsDynamic IN (0,1)),
    Notes        TEXT   -- e.g. reviewer comments from SASL consultant
);

CREATE TABLE HandJoints (
    JointID   INTEGER PRIMARY KEY,
    JointName TEXT NOT NULL UNIQUE
);

CREATE TABLE Participants (
    ParticipantID   TEXT PRIMARY KEY,
    DominantHand    TEXT NOT NULL CHECK (DominantHand IN ('Left','Right')),
    SignerBackground TEXT NOT NULL CHECK (SignerBackground IN ('Deaf','HearingFluent','Learner')),
    AgeBand         TEXT,          -- e.g. '18-25' (avoid exact DOB)
    HandLengthMm    REAL,          -- for scale normalisation
    ConsentRef      TEXT NOT NULL  -- reference to signed consent (POPIA)
);

-- ---------- CAPTURE TABLES ----------
CREATE TABLE Recordings (               -- one "take" of one sign
    RecordingID     INTEGER PRIMARY KEY AUTOINCREMENT,
    SignID          INTEGER NOT NULL REFERENCES Signs(SignID),
    ParticipantID   TEXT NOT NULL REFERENCES Participants(ParticipantID),
    HandUsed        TEXT NOT NULL CHECK (HandUsed IN ('Left','Right')),
    DeviceModel     TEXT NOT NULL,      -- e.g. 'Quest 3'
    SampleRateHz    REAL,
    RecordedAt      TEXT NOT NULL DEFAULT (datetime('now')),
    IsCorrectExample INTEGER NOT NULL DEFAULT 1 CHECK (IsCorrectExample IN (0,1)),
    MistakeType     TEXT,               -- for incorrect examples, e.g. 'thumb tucked wrong'
    ReviewStatus    TEXT NOT NULL DEFAULT 'Pending'
                    CHECK (ReviewStatus IN ('Pending','Approved','Rejected')),
    Notes           TEXT
);
CREATE INDEX idx_rec_sign ON Recordings(SignID);
CREATE INDEX idx_rec_participant ON Recordings(ParticipantID);

CREATE TABLE FrameData (
    FrameID        INTEGER PRIMARY KEY AUTOINCREMENT,
    RecordingID    INTEGER NOT NULL REFERENCES Recordings(RecordingID) ON DELETE CASCADE,
    SequenceNumber INTEGER NOT NULL,
    TimestampMs    REAL NOT NULL,       -- ms since start of recording
    UNIQUE (RecordingID, SequenceNumber)
);

CREATE TABLE JointCoordinates (
    FrameID   INTEGER NOT NULL REFERENCES FrameData(FrameID) ON DELETE CASCADE,
    JointID   INTEGER NOT NULL REFERENCES HandJoints(JointID),
    PosX REAL NOT NULL, PosY REAL NOT NULL, PosZ REAL NOT NULL,
    RotX REAL, RotY REAL, RotZ REAL, RotW REAL,   -- OpenXR orientation quaternion
    Radius    REAL,                                -- OpenXR joint radius
    IsTracked INTEGER NOT NULL DEFAULT 1 CHECK (IsTracked IN (0,1)),
    PRIMARY KEY (FrameID, JointID)
) WITHOUT ROWID;

-- ---------- SEED DATA ----------
WITH RECURSIVE n(i) AS (SELECT 0 UNION ALL SELECT i+1 FROM n WHERE i < 25)
INSERT INTO Signs (SignLabel, SignCategory, IsDynamic)
SELECT char(65+i), 'Letter', CASE WHEN char(65+i) IN ('J','Z') THEN 1 ELSE 0 END FROM n;

INSERT INTO Signs (SignLabel, SignCategory, IsDynamic) VALUES
('1','Number',0),('2','Number',0),('3','Number',0),('4','Number',0),('5','Number',0),
('6','Number',0),('7','Number',0),('8','Number',0),('9','Number',0),('10','Number',0);

INSERT INTO HandJoints (JointID, JointName) VALUES
(0,'Palm'),(1,'Wrist'),
(2,'ThumbMetacarpal'),(3,'ThumbProximal'),(4,'ThumbDistal'),(5,'ThumbTip'),
(6,'IndexMetacarpal'),(7,'IndexProximal'),(8,'IndexIntermediate'),(9,'IndexDistal'),(10,'IndexTip'),
(11,'MiddleMetacarpal'),(12,'MiddleProximal'),(13,'MiddleIntermediate'),(14,'MiddleDistal'),(15,'MiddleTip'),
(16,'RingMetacarpal'),(17,'RingProximal'),(18,'RingIntermediate'),(19,'RingDistal'),(20,'RingTip'),
(21,'LittleMetacarpal'),(22,'LittleProximal'),(23,'LittleIntermediate'),(24,'LittleDistal'),(25,'LittleTip');
