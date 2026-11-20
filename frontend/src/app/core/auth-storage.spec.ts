import assert from 'node:assert/strict';
import test from 'node:test';
import { usuarioEhAdmin } from './auth-storage.ts';
import type { UsuarioPerfil } from './models.ts';

test('usuarioEhAdmin identifica perfil Admin de forma case-insensitive', () => {
  const admin: UsuarioPerfil = {
    id: 1,
    userName: 'desanti',
    name: 'DeSanti Automotiva',
    email: null,
    role: 'Admin',
    isActive: true,
    createdAtUtc: '2026-11-20T12:00:00Z',
  };
  const operador: UsuarioPerfil = { ...admin, id: 2, userName: 'balcao', role: 'Operador' };

  assert.equal(usuarioEhAdmin(admin), true);
  assert.equal(usuarioEhAdmin(operador), false);
  assert.equal(usuarioEhAdmin(null), false);
});
