using System;
using System.Collections.Generic;
using Godot;

// Rail track system.
// Tracks are Catmull-Rom splines drawn freehand. Turrets and arms snap onto a
// track and become overlay-only (no grid cells). The track's two endpoints are
// wire-snappable power terminals: connect a wire and all machines riding the
// track are powered. Multiple machines per track slide independently.
public partial class Main
{
	private const float TrackSnapRadius       = 6f;    // sim cells — snap distance for placing a machine near a track
	private const float TrackMachineHitRadius = 4f;    // sim cells — hit radius for drag/detach
	private const float TrackSmoothingSpeed   = 0.02f; // track-position units per sim tick (scripted motion)

	private sealed class RailTrack
	{
		public static float RawMinDist = 1.0f;
		public static float RdpEpsilon = 1.5f;

		private const int ArcSamples = 128;
		private const int DrawSteps  = 60;

		private readonly List<Vector2> _raw = new();
		public  readonly List<Vector2> SamplePoints = new();

		private struct Seg { public Vector2 P0, P1, P2, P3; }
		private readonly List<Seg> _segs = new();

		private readonly Vector2[] _arcPts     = new Vector2[ArcSamples];
		private readonly float[]   _arcLengths = new float[ArcSamples];
		private float              _totalLength;

		public bool Powered;
		public readonly List<object> Machines = new(); // LaserTurret or RoboArm

		public Vector2 StartPoint => SamplePoints.Count > 0 ? SamplePoints[0]  : Vector2.Zero;
		public Vector2 EndPoint   => SamplePoints.Count > 0 ? SamplePoints[^1] : Vector2.Zero;
		public bool    HasCurve   => _segs.Count > 0;

		// ── Curve construction ────────────────────────────────────────────────────

		public void AddSample(Vector2 p)
		{
			if (_raw.Count > 0 && (p - _raw[^1]).LengthSquared() < RawMinDist * RawMinDist) return;
			_raw.Add(p);
			SamplePoints.Clear();
			SamplePoints.Add(_raw[0]);
			if (_raw.Count > 1)
				RdpRecurse(_raw, 0, _raw.Count - 1, RdpEpsilon * RdpEpsilon, SamplePoints);
			if (SamplePoints.Count >= 2) Rebuild();
		}

		private static void RdpRecurse(List<Vector2> pts, int lo, int hi, float epsSq, List<Vector2> result)
		{
			if (hi <= lo + 1) { result.Add(pts[hi]); return; }
			var a = pts[lo]; var b = pts[hi];
			var ab = b - a; float abLen2 = ab.LengthSquared();
			float maxSq = 0f; int maxIdx = lo + 1;
			for (int i = lo + 1; i < hi; i++)
			{
				float sq = abLen2 > 1e-12f
					? PerpendicularDistSq(pts[i], a, ab, abLen2)
					: (pts[i] - a).LengthSquared();
				if (sq > maxSq) { maxSq = sq; maxIdx = i; }
			}
			if (maxSq > epsSq) { RdpRecurse(pts, lo, maxIdx, epsSq, result); RdpRecurse(pts, maxIdx, hi, epsSq, result); }
			else result.Add(pts[hi]);
		}

		private static float PerpendicularDistSq(Vector2 p, Vector2 a, Vector2 ab, float abLen2)
		{
			float t  = Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / abLen2, 0f, 1f);
			float ex = a.X + t * ab.X - p.X, ey = a.Y + t * ab.Y - p.Y;
			return ex * ex + ey * ey;
		}

		private void Rebuild()
		{
			_segs.Clear();
			int n = SamplePoints.Count;
			for (int i = 0; i < n - 1; i++)
			{
				var p0 = i > 0     ? SamplePoints[i - 1] : 2f * SamplePoints[i] - SamplePoints[i + 1];
				var p1 = SamplePoints[i];
				var p2 = SamplePoints[i + 1];
				var p3 = i + 2 < n ? SamplePoints[i + 2] : 2f * SamplePoints[i + 1] - SamplePoints[i];
				float d01 = (p1 - p0).Length(), d12 = (p2 - p1).Length(), d23 = (p3 - p2).Length();
				if (d12 < 1e-6f) continue;
				Vector2 b1 = d01 < 1e-6f
					? p1 + (p2 - p1) / 3f
					: p1 + ((p2 - p1) / d12 - (p2 - p0) / (d01 + d12) + (p1 - p0) / d01) * (d12 / 3f);
				Vector2 b2 = d23 < 1e-6f
					? p2 - (p2 - p1) / 3f
					: p2 - ((p3 - p2) / d23 - (p3 - p1) / (d12 + d23) + (p2 - p1) / d12) * (d12 / 3f);
				_segs.Add(new Seg { P0 = p1, P1 = b1, P2 = b2, P3 = p2 });
			}
			RebuildArcTable();
		}

		private static Vector2 EvalSeg(in Seg s, float t)
		{
			float u = 1f - t;
			return u*u*u*s.P0 + 3f*u*u*t*s.P1 + 3f*u*t*t*s.P2 + t*t*t*s.P3;
		}

		// ── Arc-length parameterization ───────────────────────────────────────────

		private void RebuildArcTable()
		{
			if (_segs.Count == 0) { _totalLength = 0; return; }
			float range = _segs.Count;
			for (int i = 0; i < ArcSamples; i++)
			{
				float bt  = (float)i / (ArcSamples - 1) * range;
				int   si  = Math.Min((int)bt, _segs.Count - 1);
				float lt  = Math.Clamp(bt - si, 0f, 1f);
				_arcPts[i] = EvalSeg(_segs[si], lt);
			}
			_arcLengths[0] = 0f;
			for (int i = 1; i < ArcSamples; i++)
				_arcLengths[i] = _arcLengths[i - 1] + (_arcPts[i] - _arcPts[i - 1]).Length();
			_totalLength = _arcLengths[ArcSamples - 1];
		}

		public Vector2 GetPointAtT(float t)
		{
			if (_segs.Count == 0) return StartPoint;
			if (_totalLength < 1e-6f) return _arcPts[0];
			float target = Math.Clamp(t, 0f, 1f) * _totalLength;
			int lo = 0, hi = ArcSamples - 1;
			while (lo < hi - 1)
			{
				int mid = (lo + hi) / 2;
				if (_arcLengths[mid] <= target) lo = mid; else hi = mid;
			}
			float span = _arcLengths[hi] - _arcLengths[lo];
			float frac = span < 1e-6f ? 0f : (target - _arcLengths[lo]) / span;
			return _arcPts[lo].Lerp(_arcPts[hi], frac);
		}

		public float NearestT(Vector2 p)
		{
			if (_segs.Count == 0 || _totalLength < 1e-6f) return 0f;
			float bestDistSq = float.MaxValue, bestT = 0f;
			for (int i = 0; i < ArcSamples; i++)
			{
				float d = (_arcPts[i] - p).LengthSquared();
				if (d < bestDistSq) { bestDistSq = d; bestT = _arcLengths[i] / _totalLength; }
			}
			return Math.Clamp(bestT, 0f, 1f);
		}

		public float NearestDistSq(Vector2 p)
		{
			if (_segs.Count == 0) return float.MaxValue;
			float best = float.MaxValue;
			for (int i = 0; i < ArcSamples; i++)
			{
				float d = (_arcPts[i] - p).LengthSquared();
				if (d < best) best = d;
			}
			return best;
		}

		// ── Drawing ───────────────────────────────────────────────────────────────

		public void Draw(OverlayCanvas c, bool wip = false)
		{
			if (_segs.Count == 0) return;

			float alpha  = wip ? 0.55f : 1.0f;
			var   bedCol = new Color(0.22f, 0.22f, 0.25f, alpha);
			var   tieCol = new Color(0.35f, 0.25f, 0.15f, alpha);
			var   railCol = (Powered && !wip)
				? new Color(0.95f, 0.85f, 0.25f, alpha)
				: new Color(0.78f, 0.78f, 0.82f, alpha);

			float railOffPx     = 1.2f * Scale;
			float tieIntervalPx = 3.0f * Scale;
			float tieHalfPx     = 1.6f * Scale;

			var pts   = new Vector2[DrawSteps + 1];
			var perps = new Vector2[DrawSteps + 1];
			for (int i = 0; i <= DrawSteps; i++)
			{
				float gt  = (float)i / DrawSteps;
				pts[i]    = GetPointAtT(gt) * Scale;
				float gt2 = Math.Clamp(gt + 0.5f / DrawSteps, 0f, 1f);
				var   fwd = GetPointAtT(gt2) * Scale - pts[i];
				float len = fwd.Length();
				perps[i]  = len < 1e-6f ? Vector2.Right : new Vector2(-fwd.Y / len, fwd.X / len);
			}

			for (int i = 0; i < DrawSteps; i++)
				c.DrawLine(pts[i], pts[i + 1], bedCol, 7f);

			float accum = 0f;
			for (int i = 1; i <= DrawSteps; i++)
			{
				accum += (pts[i] - pts[i - 1]).Length();
				if (accum >= tieIntervalPx)
				{
					accum -= tieIntervalPx;
					c.DrawLine(pts[i] - perps[i] * tieHalfPx, pts[i] + perps[i] * tieHalfPx, tieCol, 4f);
				}
			}

			for (int i = 0; i < DrawSteps; i++)
			{
				c.DrawLine(pts[i] + perps[i] * railOffPx, pts[i + 1] + perps[i + 1] * railOffPx, railCol, 2.5f);
				c.DrawLine(pts[i] - perps[i] * railOffPx, pts[i + 1] - perps[i + 1] * railOffPx, railCol, 2.5f);
			}

			if (!wip)
			{
				c.DrawCircle(pts[0],           5f, railCol);
				c.DrawCircle(pts[DrawSteps],   5f, railCol);
			}
		}
	}

	// ── Track update, draw ────────────────────────────────────────────────────

	private void UpdateTracks()
	{
		// Powered state is reset and recomputed each frame by PropagateWirePower
	}

	private void DrawTracks(OverlayCanvas c)
	{
		foreach (var tr in _tracks) tr.Draw(c);
		_trackInProgress?.Draw(c, wip: true);
	}

	// ── Machine placement / attachment ────────────────────────────────────────

	private (RailTrack track, float t)? FindNearestTrackSnap(Vector2 simPos)
	{
		float bestDistSq = TrackSnapRadius * TrackSnapRadius;
		(RailTrack, float)? best = null;
		foreach (var tr in _tracks)
		{
			float dSq = tr.NearestDistSq(simPos);
			if (dSq < bestDistSq) { bestDistSq = dSq; best = (tr, tr.NearestT(simPos)); }
		}
		return best;
	}

	private void PlaceTrackTurret(RailTrack track, float trackT)
	{
		var pos = track.GetPointAtT(trackT);
		var t   = new LaserTurret
		{
			Origin       = new Vector2I((int)pos.X, (int)pos.Y),
			Track        = track,
			TrackT       = trackT,
			TargetTrackT = trackT,
		};
		_turrets.Add(t);
		track.Machines.Add(t);
	}

	private void PlaceTrackArm(RailTrack track, float trackT)
	{
		var pos = track.GetPointAtT(trackT);
		var a   = new RoboArm
		{
			Origin              = new Vector2I((int)pos.X, (int)pos.Y),
			Track               = track,
			TrackT              = trackT,
			TargetTrackT        = trackT,
			ShoulderAngle       = -MathF.PI / 2f,
			TargetShoulderAngle = -MathF.PI / 2f,
			ElbowAngle          = -MathF.PI / 2f,
			TargetElbowAngle    = -MathF.PI / 2f,
		};
		_arms.Add(a);
		track.Machines.Add(a);
		_activeArm = a;
	}

	// ── Detach ────────────────────────────────────────────────────────────────

	private void DetachTurretFromTrack(LaserTurret t)
	{
		if (t.Track == null) return;
		t.Track.Machines.Remove(t);
		t.Track = null;
		var origin = t.Origin;
		for (int dy = 0; dy < LaserTurret.BaseH; dy++)
		for (int dx = -LaserTurret.BaseHalfW; dx <= LaserTurret.BaseHalfW; dx++)
		{
			int gx = origin.X + dx, gy = origin.Y + dy;
			if (!_sim.InBounds(gx, gy)) continue;
			int idx = gy * SimW + gx;
			_sim.Grid[idx]   = (byte)(dy == 0 && dx == 0 ? Simulation.Cell.Battery : Simulation.Cell.Stone);
			_sim.Flow[idx]   = 0;
			_sim.Pinned[idx] = 1;
			t.OccupiedIndices.Add(idx);
		}
		int termRow = origin.Y + 1;
		foreach (int tx in new[] { origin.X - LaserTurret.BaseHalfW - 1, origin.X + LaserTurret.BaseHalfW + 1 })
		{
			if (!_sim.InBounds(tx, termRow)) continue;
			int idx = termRow * SimW + tx;
			_sim.Grid[idx]   = (byte)Simulation.Cell.Copper;
			_sim.Flow[idx]   = 128;
			_sim.Pinned[idx] = 1;
			t.OccupiedIndices.Add(idx);
		}
		_sim.RenderDirty = true;
	}

	private void DetachArmFromTrack(RoboArm a)
	{
		if (a.Track == null) return;
		a.Track.Machines.Remove(a);
		a.Track = null;
		var origin = a.Origin;
		for (int dy = -RoboArm.BaseHalfW; dy <= RoboArm.BaseHalfW; dy++)
		for (int dx = -RoboArm.BaseHalfW; dx <= RoboArm.BaseHalfW; dx++)
		{
			int gx = origin.X + dx, gy = origin.Y + dy;
			if (!_sim.InBounds(gx, gy)) continue;
			int idx = gy * SimW + gx;
			_sim.Grid[idx]   = (byte)Simulation.Cell.Stone;
			_sim.Flow[idx]   = 0;
			_sim.Pinned[idx] = 1;
			a.OccupiedIndices.Add(idx);
		}
		int termY = origin.Y;
		foreach (int tx in new[] { origin.X - RoboArm.BaseHalfW - 1, origin.X + RoboArm.BaseHalfW + 1 })
		{
			if (!_sim.InBounds(tx, termY)) continue;
			int idx = termY * SimW + tx;
			_sim.Grid[idx]   = (byte)Simulation.Cell.Copper;
			_sim.Flow[idx]   = 128;
			_sim.Pinned[idx] = 1;
			a.OccupiedIndices.Add(idx);
		}
		_sim.RenderDirty = true;
	}

	private void DeleteTrack(RailTrack track)
	{
		for (int i = track.Machines.Count - 1; i >= 0; i--)
		{
			if      (track.Machines[i] is LaserTurret t) DetachTurretFromTrack(t);
			else if (track.Machines[i] is RoboArm     a) DetachArmFromTrack(a);
		}
		// Remove wire nodes anchored to this track's endpoints
		for (int i = _wireNodes.Count - 1; i >= 0; i--)
		{
			if (_wireNodes[i].TrackRef != track) continue;
			foreach (int ci in _wireNodes[i].Connections)
				_wireNodes[ci].Connections.Remove(i);
			_wireNodes.RemoveAt(i);
			for (int j = 0; j < _wireNodes.Count; j++)
				for (int k = 0; k < _wireNodes[j].Connections.Count; k++)
					if (_wireNodes[j].Connections[k] > i)
						_wireNodes[j].Connections[k]--;
			if      (_wirePendingIdx == i) _wirePendingIdx = -1;
			else if (_wirePendingIdx  > i) _wirePendingIdx--;
		}
		_tracks.Remove(track);
	}

	// Returns true if handled (caller should suppress normal RMB erase).
	private bool TryTrackRmb(Vector2 simPos)
	{
		float hitSq = TrackMachineHitRadius * TrackMachineHitRadius;

		foreach (var t in _turrets)
		{
			if (t.Track == null) continue;
			if ((new Vector2(t.Origin.X + 0.5f, t.Origin.Y + 1.5f) - simPos).LengthSquared() < hitSq)
			{ DetachTurretFromTrack(t); return true; }
		}
		foreach (var a in _arms)
		{
			if (a.Track == null) continue;
			if ((new Vector2(a.Origin.X + 0.5f, a.Origin.Y + 0.5f) - simPos).LengthSquared() < hitSq)
			{ DetachArmFromTrack(a); return true; }
		}

		float deleteRadSq = (TrackSnapRadius + 1f) * (TrackSnapRadius + 1f);
		for (int i = _tracks.Count - 1; i >= 0; i--)
		{
			if (_tracks[i].NearestDistSq(simPos) < deleteRadSq)
			{ DeleteTrack(_tracks[i]); return true; }
		}
		return false;
	}

	private object FindNearestTrackMachine(Vector2 simPos)
	{
		float bestSq = TrackMachineHitRadius * TrackMachineHitRadius;
		object best  = null;
		foreach (var t in _turrets)
		{
			if (t.Track == null) continue;
			float dSq = (new Vector2(t.Origin.X + 0.5f, t.Origin.Y + 1.5f) - simPos).LengthSquared();
			if (dSq < bestSq) { bestSq = dSq; best = t; }
		}
		foreach (var a in _arms)
		{
			if (a.Track == null) continue;
			float dSq = (new Vector2(a.Origin.X + 0.5f, a.Origin.Y + 0.5f) - simPos).LengthSquared();
			if (dSq < bestSq) { bestSq = dSq; best = a; }
		}
		return best;
	}

	private void UpdateTrackMachineDrag(Vector2 simPos)
	{
		if (_trackDragMachine is LaserTurret t && t.Track != null)
			t.TargetTrackT = t.Track.NearestT(simPos);
		else if (_trackDragMachine is RoboArm a && a.Track != null)
			a.TargetTrackT = a.Track.NearestT(simPos);
		_sim.RenderDirty = true;
	}
}
