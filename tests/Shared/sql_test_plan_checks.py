"""Offline checks for the generated test setup. No SQL Server connection."""
from pathlib import Path
import ast
import hashlib
import itertools
import sqlite3
import subprocess
import sys

root = Path(__file__).resolve().parents[2]
plan = root / 'Database/TestPlan'
tree = ast.parse((plan / 'prepare_plan.py').read_text(encoding='utf-8-sig'))
constants = {}
for statement in tree.body:
    if isinstance(statement, ast.Assign):
        for target in statement.targets:
            if isinstance(target, ast.Name) and target.id in ('SET_OPTIONS', 'SESSION_ACCESS_PREDICATE', 'WRITER_PROFILE_PREDICATE'):
                constants[target.id] = ast.literal_eval(statement.value)
policy = constants['SESSION_ACCESS_PREDICATE']
cases = 0
with sqlite3.connect(':memory:') as connection:
    for version, admin, state, performance in itertools.product([None, 0, 1, 15, 16, 17], [0, 1], [0, 1], [0, 1]):
        actual = connection.execute(
            'SELECT CASE WHEN ' + policy + ' THEN 1 ELSE 0 END FROM '
            '(SELECT ? MajorVersion,? IsSysadmin,? CanViewState,? CanViewPerformance)',
            (version, admin, state, performance)).fetchone()[0]
        expected = version is not None and version > 0 and (admin == 1 or (performance == 1 if version >= 16 else state == 1))
        assert actual == int(expected), (version, admin, state, performance)
        cases += 1
    writer_cases = 0
    writer_policy = constants['WRITER_PROFILE_PREDICATE']
    for values in itertools.product([None, 0, 1], repeat=5):
        actual = connection.execute('SELECT CASE WHEN ' + writer_policy + ' THEN 1 ELSE 0 END FROM '
            '(SELECT ? IsSysadmin,? IsDbcreator,? IsDbOwner,? HasControl,? HasAlter)', values).fetchone()[0]
        assert actual == int(all(v == 0 for v in values)), values
        writer_cases += 1

for filename, first_write in [('01_CreateAndSchema.sql', "EXEC(N'CREATE DATABASE"), ('02_FakeFixtures.sql', 'INSERT dbo.')]:
    text = (plan / filename).read_text(encoding='utf-8-sig')
    assert constants['SET_OPTIONS'] in text
    assert text.index(constants['SET_OPTIONS']) < text.index(first_write)
    assert 'IF DB_NAME()<>' in text or "DB_NAME()=N'FlightBooking_Hieu_ManagementTests'" in text
for path in [plan / '02_FakeFixtures.sql', plan / '03_Preflight.sql', root / 'tests/Shared/SqlTestPreflight.sql']:
    text = path.read_text(encoding='utf-8-sig')
    assert policy in text and 'FROM sys.dm_exec_sessions' in text
    assert writer_policy in text and "N'main_write_isolation'" in text
    assert "'UPDATE',ColumnName,'COLUMN'" in text
assert hashlib.sha256((plan / 'History/01_CreateAndSchema-6EE11AF9.sql').read_bytes()).hexdigest().upper() == '6EE11AF95A66B9603CFCA1C963D0D66F0234BF89DA1422B6FBB29011A09A220A'
subprocess.run([sys.executable, str(plan / 'prepare_plan.py'), '--check'], check=True, cwd=root)
print(f'PASS: {cases} session-policy + {writer_cases} writer-profile cases; SET guards, synchronized copies, archive and deterministic outputs. No SQL Server connection.')
