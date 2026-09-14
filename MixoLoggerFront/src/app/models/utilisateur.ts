import { Guid } from "../core/base-models";

/** L'utilisateur de la session courante, tel que renvoyé par `GET /api/auth/moi`. */
export interface Utilisateur {
	id: Guid;
	identifiant: string;
	nomAffiche: string;
}
