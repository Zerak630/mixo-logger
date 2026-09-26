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

export const MyPreset = definePreset(AuraReduit, {
	semantic: {
		primary: {
			50: '{purple.50}',
			100: '{purple.100}',
			200: '{purple.200}',
			300: '{purple.300}',
			400: '{purple.400}',
			500: '{purple.500}',
			600: '{purple.600}',
			700: '{purple.700}',
			800: '{purple.800}',
			900: '{purple.900}',
			950: '{purple.950}'
		}
	}
});
