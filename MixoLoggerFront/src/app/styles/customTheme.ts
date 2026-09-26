import { definePreset } from "@openng/optimus-ui-themes";
import type { ExtendedCSS } from "@openng/optimus-ui-themes/types";
import autocomplete from "@openng/optimus-ui-themes/aura/autocomplete";
import avatar from "@openng/optimus-ui-themes/aura/avatar";
import badge from "@openng/optimus-ui-themes/aura/badge";
import base from "@openng/optimus-ui-themes/aura/base";
import button from "@openng/optimus-ui-themes/aura/button";
import chip from "@openng/optimus-ui-themes/aura/chip";
import confirmpopup from "@openng/optimus-ui-themes/aura/confirmpopup";
import cssAura from "@openng/optimus-ui-themes/aura/css";
import iconfield from "@openng/optimus-ui-themes/aura/iconfield";
import inputnumber from "@openng/optimus-ui-themes/aura/inputnumber";
import inputtext from "@openng/optimus-ui-themes/aura/inputtext";
import rating from "@openng/optimus-ui-themes/aura/rating";
import ripple from "@openng/optimus-ui-themes/aura/ripple";
import select from "@openng/optimus-ui-themes/aura/select";
import selectbutton from "@openng/optimus-ui-themes/aura/selectbutton";
import textarea from "@openng/optimus-ui-themes/aura/textarea";
import toast from "@openng/optimus-ui-themes/aura/toast";
import togglebutton from "@openng/optimus-ui-themes/aura/togglebutton";
import toggleswitch from "@openng/optimus-ui-themes/aura/toggleswitch";
import tooltip from "@openng/optimus-ui-themes/aura/tooltip";
import virtualscroller from "@openng/optimus-ui-themes/aura/virtualscroller";

/**
 * Aura, réduit aux composants que l'application utilise.
 *
 * Le preset complet (`@openng/optimus-ui-themes/aura`) embarque les jetons de près de 90 composants,
 * tableaux, calendriers et arbres compris : 130 kB du chargement initial, pour une douzaine utilisés.
 * Un composant ajouté à l'application doit l'être ici aussi, avec ceux qu'il utilise en interne
 * (selectbutton → togglebutton, select → tooltip, button → badge, autocomplete → chip,
 * virtualscroller) : sans ses
 * jetons, il s'affiche sans style, sans erreur.
 */
const AuraReduit = {
	...base,
	components: {
		autocomplete,
		avatar,
		badge,
		button,
		chip,
		confirmpopup,
		iconfield,
		inputnumber,
		inputtext,
		rating,
		ripple,
		select,
		selectbutton,
		textarea,
		toast,
		togglebutton,
		toggleswitch,
		tooltip,
		virtualscroller
	},
	// Le module exporte ce CSS par défaut, mais ses types déclarent un export nommé : on le type à la main.
	css: cssAura as unknown as ExtendedCSS
};

/**
 * Palette de l'application (docs/MVP.md §5.1), source unique des couleurs : les écrans lisent les
 * jetons `--p-*` qui en découlent, directement ou via leurs alias `--mixo-*` (src/styles.scss).
 *
 * - `electrique` : le violet électrique #8A2BE2 en 500, #6A0DAD en 700 ;
 * - `anthracite` : les gris du thème sombre, fond de page #0A0A0A en 950, cartes #1E1E1E en 900.
 *
 * Le violet n'atteint pas le contraste AA pour du texte sur le fond : il colore les fonds de
 * boutons (texte blanc, 6:1), les bordures et les icônes, jamais un texte courant.
 */
const electrique = {
	50: '#f5edfd',
	100: '#ead9fb',
	200: '#d5b3f6',
	300: '#bf8cf1',
	400: '#a45de9',
	500: '#8A2BE2',
	600: '#7a1cc7',
	700: '#6A0DAD',
	800: '#560b8c',
	900: '#420869',
	950: '#2b0545'
};

const anthracite = {
	0: '#ffffff',
	50: '#fafafa',
	100: '#F5F5F5',
	200: '#E0E0E0',
	300: '#d4d4d4',
	400: '#a3a3a3',
	500: '#737373',
	600: '#525252',
	700: '#3a3a3a',
	800: '#2a2a2a',
	900: '#1E1E1E',
	950: '#0A0A0A'
};

export const MyPreset = definePreset(AuraReduit, {
	primitive: { electrique, anthracite },
	semantic: {
		primary: Object.fromEntries(Object.keys(electrique).map(nuance => [nuance, `{electrique.${nuance}}`])),
		// Le thème est toujours sombre (app.config.ts) : seul ce schéma est redéfini.
		colorScheme: {
			dark: {
				surface: Object.fromEntries(Object.keys(anthracite).map(nuance => [nuance, `{anthracite.${nuance}}`])),
				primary: {
					color: '{primary.500}',
					contrastColor: '#ffffff',
					hoverColor: '{primary.600}',
					activeColor: '{primary.700}'
				},
				text: {
					color: '{surface.100}',
					hoverColor: '{surface.0}',
					mutedColor: '{surface.400}',
					hoverMutedColor: '{surface.300}'
				}
			}
		}
	}
});
