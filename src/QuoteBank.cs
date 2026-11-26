// FlatCat - 金句库：诗句 / 哲思 / 原创金句，全部内置，不联网
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
//
// 数据来源：
//   诗句、哲思为公有领域经典原文（唐宋诗词、诗经、楚辞、论语、道德经、庄子、
//   菜根谭、幽梦影、围炉夜话等，作者逝世均逾 70 年）。
//   其中诗经 / 论语 / 楚辞 / 幽梦影 / 纳兰性德 等条目已与 chinese-poetry/chinese-poetry
//   （MIT License）收录的原文逐条比对过，出处以此为准。
//   金句为 FlatCat 原创短句，随项目一同以 MIT 发布。
//
// 未采用 hitokoto-osc/sentences-bundle：该项目为 AGPL-3.0，与本项目 MIT 不兼容。
//
// 本文件由 tools/gen_quotebank.py 生成；用户自定义条目运行时另行拼进池子，不写在这里。
using System;
using System.Collections.Generic;

namespace FlatCat
{
    /// <summary>一条金句：正文 + 作者 + 出处 + 分类位。</summary>
    internal sealed class Quote
    {
        public readonly string Text;
        public readonly string Author;
        public readonly string Source;
        public readonly int Cat;

        public Quote(string text, string author, string source, int cat)
        {
            Text = text;
            Author = author;
            Source = source;
            Cat = cat;
        }

        /// <summary>气泡第二行的小字署名；原创金句不署名。</summary>
        public string Credit
        {
            get
            {
                if (Cat == QuoteBank.CatLine) return "";
                if (string.IsNullOrEmpty(Author) || Author == "佚名") return Source == null ? "" : Source;
                if (string.IsNullOrEmpty(Source)) return Author;
                return Author + " · " + Source;
            }
        }
    }

    internal static class QuoteBank
    {
        public const int CatThought = 1;   // 哲思
        public const int CatPoem = 2;      // 诗句
        public const int CatLine = 4;      // 原创金句
        public const int CatCustom = 8;    // 用户自己添加 / 导入的内容库
        public const int CatAll = CatThought | CatPoem | CatLine | CatCustom;
        /// <summary>内置条目的分类范围（不含自定义）。</summary>
        public const int CatBuiltIn = CatThought | CatPoem | CatLine;
        /// <summary>自定义条目上限，防止配置文件被塞爆。</summary>
        public const int CustomLimit = 5000;

        public static readonly Quote[] All = new Quote[]
        {
            new Quote("海上生明月，天涯共此时", "张九龄", "望月怀远", CatPoem),
            new Quote("床前明月光，疑是地上霜", "李白", "静夜思", CatPoem),
            new Quote("会当凌绝顶，一览众山小", "杜甫", "望岳", CatPoem),
            new Quote("长风破浪会有时，直挂云帆济沧海", "李白", "行路难", CatPoem),
            new Quote("天生我材必有用，千金散尽还复来", "李白", "将进酒", CatPoem),
            new Quote("安能摧眉折腰事权贵，使我不得开心颜", "李白", "梦游天姥吟留别", CatPoem),
            new Quote("两个黄鹂鸣翠柳，一行白鹭上青天", "杜甫", "绝句", CatPoem),
            new Quote("无边落木萧萧下，不尽长江滚滚来", "杜甫", "登高", CatPoem),
            new Quote("随风潜入夜，润物细无声", "杜甫", "春夜喜雨", CatPoem),
            new Quote("读书破万卷，下笔如有神", "杜甫", "奉赠韦左丞丈", CatPoem),
            new Quote("春眠不觉晓，处处闻啼鸟", "孟浩然", "春晓", CatPoem),
            new Quote("野旷天低树，江清月近人", "孟浩然", "宿建德江", CatPoem),
            new Quote("欲穷千里目，更上一层楼", "王之涣", "登鹳雀楼", CatPoem),
            new Quote("白日依山尽，黄河入海流", "王之涣", "登鹳雀楼", CatPoem),
            new Quote("羌笛何须怨杨柳，春风不度玉门关", "王之涣", "凉州词", CatPoem),
            new Quote("大漠孤烟直，长河落日圆", "王维", "使至塞上", CatPoem),
            new Quote("明月松间照，清泉石上流", "王维", "山居秋暝", CatPoem),
            new Quote("行到水穷处，坐看云起时", "王维", "终南别业", CatPoem),
            new Quote("空山新雨后，天气晚来秋", "王维", "山居秋暝", CatPoem),
            new Quote("独在异乡为异客，每逢佳节倍思亲", "王维", "九月九日忆山东兄弟", CatPoem),
            new Quote("劝君更尽一杯酒，西出阳关无故人", "王维", "送元二使安西", CatPoem),
            new Quote("红豆生南国，春来发几枝", "王维", "相思", CatPoem),
            new Quote("春江潮水连海平，海上明月共潮生", "张若虚", "春江花月夜", CatPoem),
            new Quote("不知细叶谁裁出，二月春风似剪刀", "贺知章", "咏柳", CatPoem),
            new Quote("少小离家老大回，乡音无改鬓毛衰", "贺知章", "回乡偶书", CatPoem),
            new Quote("桃花潭水深千尺，不及汪伦送我情", "李白", "赠汪伦", CatPoem),
            new Quote("孤帆远影碧空尽，唯见长江天际流", "李白", "黄鹤楼送孟浩然之广陵", CatPoem),
            new Quote("举杯邀明月，对影成三人", "李白", "月下独酌", CatPoem),
            new Quote("抽刀断水水更流，举杯消愁愁更愁", "李白", "宣州谢朓楼饯别校书叔云", CatPoem),
            new Quote("山随平野尽，江入大荒流", "李白", "渡荆门送别", CatPoem),
            new Quote("云想衣裳花想容，春风拂槛露华浓", "李白", "清平调", CatPoem),
            new Quote("两岸猿声啼不住，轻舟已过万重山", "李白", "早发白帝城", CatPoem),
            new Quote("国破山河在，城春草木深", "杜甫", "春望", CatPoem),
            new Quote("露从今夜白，月是故乡明", "杜甫", "月夜忆舍弟", CatPoem),
            new Quote("朱门酒肉臭，路有冻死骨", "杜甫", "自京赴奉先县咏怀", CatPoem),
            new Quote("细雨鱼儿出，微风燕子斜", "杜甫", "水槛遣心", CatPoem),
            new Quote("迟日江山丽，春风花草香", "杜甫", "绝句", CatPoem),
            new Quote("一骑红尘妃子笑，无人知是荔枝来", "杜牧", "过华清宫", CatPoem),
            new Quote("商女不知亡国恨，隔江犹唱后庭花", "杜牧", "泊秦淮", CatPoem),
            new Quote("停车坐爱枫林晚，霜叶红于二月花", "杜牧", "山行", CatPoem),
            new Quote("清明时节雨纷纷，路上行人欲断魂", "杜牧", "清明", CatPoem),
            new Quote("身无彩凤双飞翼，心有灵犀一点通", "李商隐", "无题", CatPoem),
            new Quote("春蚕到死丝方尽，蜡炬成灰泪始干", "李商隐", "无题", CatPoem),
            new Quote("夕阳无限好，只是近黄昏", "李商隐", "乐游原", CatPoem),
            new Quote("何当共剪西窗烛，却话巴山夜雨时", "李商隐", "夜雨寄北", CatPoem),
            new Quote("天街小雨润如酥，草色遥看近却无", "韩愈", "早春呈水部张十八员外", CatPoem),
            new Quote("业精于勤，荒于嬉", "韩愈", "进学解", CatPoem),
            new Quote("沉舟侧畔千帆过，病树前头万木春", "刘禹锡", "酬乐天扬州初逢席上见赠", CatPoem),
            new Quote("晴空一鹤排云上，便引诗情到碧霄", "刘禹锡", "秋词", CatPoem),
            new Quote("东边日出西边雨，道是无晴却有晴", "刘禹锡", "竹枝词", CatPoem),
            new Quote("旧时王谢堂前燕，飞入寻常百姓家", "刘禹锡", "乌衣巷", CatPoem),
            new Quote("千山鸟飞绝，万径人踪灭", "柳宗元", "江雪", CatPoem),
            new Quote("孤舟蓑笠翁，独钓寒江雪", "柳宗元", "江雪", CatPoem),
            new Quote("曾经沧海难为水，除却巫山不是云", "元稹", "离思", CatPoem),
            new Quote("不是花中偏爱菊，此花开尽更无花", "元稹", "菊花", CatPoem),
            new Quote("野火烧不尽，春风吹又生", "白居易", "赋得古原草送别", CatPoem),
            new Quote("同是天涯沦落人，相逢何必曾相识", "白居易", "琵琶行", CatPoem),
            new Quote("日出江花红胜火，春来江水绿如蓝", "白居易", "忆江南", CatPoem),
            new Quote("乱花渐欲迷人眼，浅草才能没马蹄", "白居易", "钱塘湖春行", CatPoem),
            new Quote("在天愿作比翼鸟，在地愿为连理枝", "白居易", "长恨歌", CatPoem),
            new Quote("忽如一夜春风来，千树万树梨花开", "岑参", "白雪歌送武判官归京", CatPoem),
            new Quote("莫愁前路无知己，天下谁人不识君", "高适", "别董大", CatPoem),
            new Quote("春潮带雨晚来急，野渡无人舟自横", "韦应物", "滁州西涧", CatPoem),
            new Quote("姑苏城外寒山寺，夜半钟声到客船", "张继", "枫桥夜泊", CatPoem),
            new Quote("月落乌啼霜满天，江枫渔火对愁眠", "张继", "枫桥夜泊", CatPoem),
            new Quote("黑云压城城欲摧，甲光向日金鳞开", "李贺", "雁门太守行", CatPoem),
            new Quote("采菊东篱下，悠然见南山", "陶渊明", "饮酒", CatPoem),
            new Quote("落红不是无情物，化作春泥更护花", "龚自珍", "己亥杂诗", CatPoem),
            new Quote("我劝天公重抖擞，不拘一格降人才", "龚自珍", "己亥杂诗", CatPoem),
            new Quote("江山代有才人出，各领风骚数百年", "赵翼", "论诗", CatPoem),
            new Quote("春宵一刻值千金，花有清香月有阴", "苏轼", "春宵", CatPoem),
            new Quote("竹外桃花三两枝，春江水暖鸭先知", "苏轼", "惠崇春江晚景", CatPoem),
            new Quote("欲把西湖比西子，淡妆浓抹总相宜", "苏轼", "饮湖上初晴后雨", CatPoem),
            new Quote("不识庐山真面目，只缘身在此山中", "苏轼", "题西林壁", CatPoem),
            new Quote("人有悲欢离合，月有阴晴圆缺", "苏轼", "水调歌头", CatPoem),
            new Quote("但愿人长久，千里共婵娟", "苏轼", "水调歌头", CatPoem),
            new Quote("大江东去，浪淘尽，千古风流人物", "苏轼", "念奴娇·赤壁怀古", CatPoem),
            new Quote("十年生死两茫茫，不思量，自难忘", "苏轼", "江城子", CatPoem),
            new Quote("莫听穿林打叶声，何妨吟啸且徐行", "苏轼", "定风波", CatPoem),
            new Quote("竹杖芒鞋轻胜马，一蓑烟雨任平生", "苏轼", "定风波", CatPoem),
            new Quote("世事一场大梦，人生几度秋凉", "苏轼", "西江月", CatPoem),
            new Quote("枝上柳绵吹又少，天涯何处无芳草", "苏轼", "蝶恋花", CatPoem),
            new Quote("寻寻觅觅，冷冷清清，凄凄惨惨戚戚", "李清照", "声声慢", CatPoem),
            new Quote("生当作人杰，死亦为鬼雄", "李清照", "夏日绝句", CatPoem),
            new Quote("知否，知否，应是绿肥红瘦", "李清照", "如梦令", CatPoem),
            new Quote("花自飘零水自流", "李清照", "一剪梅", CatPoem),
            new Quote("蓦然回首，那人却在灯火阑珊处", "辛弃疾", "青玉案·元夕", CatPoem),
            new Quote("稻花香里说丰年，听取蛙声一片", "辛弃疾", "西江月", CatPoem),
            new Quote("醉里挑灯看剑，梦回吹角连营", "辛弃疾", "破阵子", CatPoem),
            new Quote("少年不识愁滋味，爱上层楼", "辛弃疾", "丑奴儿", CatPoem),
            new Quote("青山遮不住，毕竟东流去", "辛弃疾", "菩萨蛮", CatPoem),
            new Quote("山重水复疑无路，柳暗花明又一村", "陆游", "游山西村", CatPoem),
            new Quote("夜阑卧听风吹雨，铁马冰河入梦来", "陆游", "十一月四日风雨大作", CatPoem),
            new Quote("纸上得来终觉浅，绝知此事要躬行", "陆游", "冬夜读书示子聿", CatPoem),
            new Quote("小荷才露尖尖角，早有蜻蜓立上头", "杨万里", "小池", CatPoem),
            new Quote("接天莲叶无穷碧，映日荷花别样红", "杨万里", "晓出净慈寺送林子方", CatPoem),
            new Quote("人生自古谁无死，留取丹心照汗青", "文天祥", "过零丁洋", CatPoem),
            new Quote("人生若只如初见，何事秋风悲画扇", "纳兰性德", "木兰花令", CatPoem),
            new Quote("山一程，水一程，身向榆关那畔行", "纳兰性德", "长相思", CatPoem),
            new Quote("谁念西风独自凉，萧萧黄叶闭疏窗", "纳兰性德", "浣溪沙", CatPoem),
            new Quote("关关雎鸠，在河之洲", "佚名", "诗经·周南·关雎", CatPoem),
            new Quote("所谓伊人，在水一方", "佚名", "诗经·秦风·蒹葭", CatPoem),
            new Quote("蒹葭苍苍，白露为霜", "佚名", "诗经·秦风·蒹葭", CatPoem),
            new Quote("桃之夭夭，灼灼其华", "佚名", "诗经·周南·桃夭", CatPoem),
            new Quote("昔我往矣，杨柳依依", "佚名", "诗经·小雅·采薇", CatPoem),
            new Quote("投我以木桃，报之以琼瑶", "佚名", "诗经·卫风·木瓜", CatPoem),
            new Quote("学而时习之，不亦说乎", "孔子", "论语·学而", CatThought),
            new Quote("三人行，必有我师焉", "孔子", "论语·述而", CatThought),
            new Quote("君子坦荡荡，小人长戚戚", "孔子", "论语·述而", CatThought),
            new Quote("己所不欲，勿施于人", "孔子", "论语·颜渊", CatThought),
            new Quote("知之为知之，不知为不知，是知也", "孔子", "论语·为政", CatThought),
            new Quote("岁寒，然后知松柏之后凋也", "孔子", "论语·子罕", CatThought),
            new Quote("逝者如斯夫，不舍昼夜", "孔子", "论语·子罕", CatThought),
            new Quote("不患人之不己知，患不知人也", "孔子", "论语·学而", CatThought),
            new Quote("见贤思齐焉，见不贤而内自省也", "孔子", "论语·里仁", CatThought),
            new Quote("知之者不如好之者，好之者不如乐之者", "孔子", "论语·雍也", CatThought),
            new Quote("德不孤，必有邻", "孔子", "论语·里仁", CatThought),
            new Quote("君子和而不同，小人同而不和", "孔子", "论语·子路", CatThought),
            new Quote("不义而富且贵，于我如浮云", "孔子", "论语·述而", CatThought),
            new Quote("敏而好学，不耻下问", "孔子", "论语·公冶长", CatThought),
            new Quote("君子欲讷于言而敏于行", "孔子", "论语·里仁", CatThought),
            new Quote("人无远虑，必有近忧", "孔子", "论语·卫灵公", CatThought),
            new Quote("工欲善其事，必先利其器", "孔子", "论语·卫灵公", CatThought),
            new Quote("学而不思则罔，思而不学则殆", "孔子", "论语·为政", CatThought),
            new Quote("温故而知新，可以为师矣", "孔子", "论语·为政", CatThought),
            new Quote("君子求诸己，小人求诸人", "孔子", "论语·卫灵公", CatThought),
            new Quote("朝闻道，夕死可矣", "孔子", "论语·里仁", CatThought),
            new Quote("士不可以不弘毅，任重而道远", "曾子", "论语·泰伯", CatThought),
            new Quote("上善若水", "老子", "道德经", CatThought),
            new Quote("知人者智，自知者明", "老子", "道德经", CatThought),
            new Quote("千里之行，始于足下", "老子", "道德经", CatThought),
            new Quote("大巧若拙，大辩若讷", "老子", "道德经", CatThought),
            new Quote("祸兮福之所倚，福兮祸之所伏", "老子", "道德经", CatThought),
            new Quote("少则得，多则惑", "老子", "道德经", CatThought),
            new Quote("为学日益，为道日损", "老子", "道德经", CatThought),
            new Quote("知足不辱，知止不殆", "老子", "道德经", CatThought),
            new Quote("柔弱胜刚强", "老子", "道德经", CatThought),
            new Quote("夫唯不争，故天下莫能与之争", "老子", "道德经", CatThought),
            new Quote("大方无隅，大器晚成", "老子", "道德经", CatThought),
            new Quote("天下难事，必作于易", "老子", "道德经", CatThought),
            new Quote("千里之堤，溃于蚁穴", "韩非", "韩非子·喻老", CatThought),
            new Quote("吾生也有涯，而知也无涯", "庄子", "庄子·养生主", CatThought),
            new Quote("人生天地之间，若白驹之过隙", "庄子", "庄子·知北游", CatThought),
            new Quote("天地有大美而不言", "庄子", "庄子·知北游", CatThought),
            new Quote("相濡以沫，不如相忘于江湖", "庄子", "庄子·大宗师", CatThought),
            new Quote("夏虫不可以语于冰", "庄子", "庄子·秋水", CatThought),
            new Quote("井蛙不可以语于海", "庄子", "庄子·秋水", CatThought),
            new Quote("至人之用心若镜，不将不迎", "庄子", "庄子·应帝王", CatThought),
            new Quote("大知闲闲，小知间间", "庄子", "庄子·齐物论", CatThought),
            new Quote("宠辱不惊，闲看庭前花开花落", "洪应明", "菜根谭", CatThought),
            new Quote("径路窄处，留一步与人行", "洪应明", "菜根谭", CatThought),
            new Quote("处世让一步为高，退步即进步的张本", "洪应明", "菜根谭", CatThought),
            new Quote("风来疏竹，风过而竹不留声", "洪应明", "菜根谭", CatThought),
            new Quote("岁月本长，而忙者自促", "洪应明", "菜根谭", CatThought),
            new Quote("势利纷华，不近者为洁", "洪应明", "菜根谭", CatThought),
            new Quote("读经宜冬，其神专也", "张潮", "幽梦影", CatThought),
            new Quote("少年读书，如隙中窥月", "张潮", "幽梦影", CatThought),
            new Quote("花不可以无蝶，山不可以无泉", "张潮", "幽梦影", CatThought),
            new Quote("因雪想高士，因花想美人", "张潮", "幽梦影", CatThought),
            new Quote("梅令人高，兰令人幽", "张潮", "幽梦影", CatThought),
            new Quote("情必近于痴而始真", "张潮", "幽梦影", CatThought),
            new Quote("春听鸟声，夏听蝉声", "张潮", "幽梦影", CatThought),
            new Quote("贫无可奈惟求俭，拙亦何妨只要勤", "王永彬", "围炉夜话", CatThought),
            new Quote("人品之不高，总为一利字看不破", "王永彬", "围炉夜话", CatThought),
            new Quote("大学之道，在明明德", "曾子", "礼记·大学", CatThought),
            new Quote("苟日新，日日新，又日新", "商汤", "礼记·大学", CatThought),
            new Quote("凡事豫则立，不豫则废", "子思", "礼记·中庸", CatThought),
            new Quote("博学之，审问之，慎思之，明辨之", "子思", "礼记·中庸", CatThought),
            new Quote("天行健，君子以自强不息", "佚名", "周易·乾", CatThought),
            new Quote("地势坤，君子以厚德载物", "佚名", "周易·坤", CatThought),
            new Quote("穷则变，变则通，通则久", "佚名", "周易·系辞", CatThought),
            new Quote("静以修身，俭以养德", "诸葛亮", "诫子书", CatThought),
            new Quote("非淡泊无以明志，非宁静无以致远", "诸葛亮", "诫子书", CatThought),
            new Quote("不积跬步，无以至千里", "荀子", "荀子·劝学", CatThought),
            new Quote("锲而不舍，金石可镂", "荀子", "荀子·劝学", CatThought),
            new Quote("青，取之于蓝，而青于蓝", "荀子", "荀子·劝学", CatThought),
            new Quote("生于忧患，死于安乐", "孟子", "孟子·告子下", CatThought),
            new Quote("富贵不能淫，贫贱不能移", "孟子", "孟子·滕文公下", CatThought),
            new Quote("老吾老，以及人之老", "孟子", "孟子·梁惠王上", CatThought),
            new Quote("尽信书，则不如无书", "孟子", "孟子·尽心下", CatThought),
            new Quote("得道者多助，失道者寡助", "孟子", "孟子·公孙丑下", CatThought),
            new Quote("路漫漫其修远兮，吾将上下而求索", "屈原", "楚辞·离骚", CatThought),
            new Quote("亦余心之所善兮，虽九死其犹未悔", "屈原", "楚辞·离骚", CatThought),
            new Quote("尺有所短，寸有所长", "佚名", "楚辞·卜居", CatThought),
            new Quote("精诚所至，金石为开", "王充", "论衡", CatThought),
            new Quote("其实地上本没有路，走的人多了，也便成了路", "鲁迅", "故乡", CatThought),
            new Quote("不在沉默中爆发，就在沉默中灭亡", "鲁迅", "记念刘和珍君", CatThought),
            new Quote("惟沉默是最高的轻蔑", "鲁迅", "且介亭杂文末编", CatThought),
            new Quote("人生得一知己足矣，斯世当以同怀视之", "鲁迅", "赠瞿秋白", CatThought),
            new Quote("凡不可说的，应当沉默", "维特根斯坦", "逻辑哲学论（自译）", CatThought),
            new Quote("把今天过好，就是最好的长期主义。", "FlatCat", "原创", CatLine),
            new Quote("慢一点，别把日子过成待办清单。", "FlatCat", "原创", CatLine),
            new Quote("你不需要赢过所有人，只要赢过昨天的自己。", "FlatCat", "原创", CatLine),
            new Quote("专注做一件小事，胜过焦虑地计划十件。", "FlatCat", "原创", CatLine),
            new Quote("先把手头这一行写完，再想后面十行。", "FlatCat", "原创", CatLine),
            new Quote("休息不是偷懒，是给下一段专注留位置。", "FlatCat", "原创", CatLine),
            new Quote("把难的事拆小，它就没那么难了。", "FlatCat", "原创", CatLine),
            new Quote("今天的进度条，比昨天的完美计划更值钱。", "FlatCat", "原创", CatLine),
            new Quote("不必等到状态好，动起来状态会自己来。", "FlatCat", "原创", CatLine),
            new Quote("你不是懒，你只是还没找到开始的那一秒。", "FlatCat", "原创", CatLine),
            new Quote("屏幕看久了，抬头看看窗外。", "FlatCat", "原创", CatLine),
            new Quote("喝水这件事，值得被提醒一次。", "FlatCat", "原创", CatLine),
            new Quote("坐直一点，肩会谢谢你。", "FlatCat", "原创", CatLine),
            new Quote("眼睛累了就闭十秒，比滴眼药水省事。", "FlatCat", "原创", CatLine),
            new Quote("把手机扣过去，世界会安静一点。", "FlatCat", "原创", CatLine),
            new Quote("焦虑的时候，先做一件能在五分钟内做完的事。", "FlatCat", "原创", CatLine),
            new Quote("允许自己今天只完成一半。", "FlatCat", "原创", CatLine),
            new Quote("做不好也没关系，做完比做好更重要。", "FlatCat", "原创", CatLine),
            new Quote("别用别人的进度衡量自己的今天。", "FlatCat", "原创", CatLine),
            new Quote("你正在成为的样子，比你现在的位置重要。", "FlatCat", "原创", CatLine),
            new Quote("把注意力花在哪里，日子就长成什么样。", "FlatCat", "原创", CatLine),
            new Quote("少刷一会儿，多睡一会儿。", "FlatCat", "原创", CatLine),
            new Quote("早睡是最便宜的自我投资。", "FlatCat", "原创", CatLine),
            new Quote("累了就停，硬扛出来的效率是假的。", "FlatCat", "原创", CatLine),
            new Quote("情绪来了先不决策，等它过去。", "FlatCat", "原创", CatLine),
            new Quote("生气的时候少说一句，之后少后悔十句。", "FlatCat", "原创", CatLine),
            new Quote("把「我不行」换成「我还没会」。", "FlatCat", "原创", CatLine),
            new Quote("与其纠结选哪条路，不如先走五十米。", "FlatCat", "原创", CatLine),
            new Quote("犹豫的成本，通常比试错更高。", "FlatCat", "原创", CatLine),
            new Quote("做完再优化，别在草稿上雕花。", "FlatCat", "原创", CatLine),
            new Quote("会议开不完的时候，先问一句：结论是什么？", "FlatCat", "原创", CatLine),
            new Quote("能被写下来的事，才不容易被忘掉。", "FlatCat", "原创", CatLine),
            new Quote("清单不需要很长，三条就够开工。", "FlatCat", "原创", CatLine),
            new Quote("关掉一个通知，专注就多一分。", "FlatCat", "原创", CatLine),
            new Quote("给重要的事留一段没人打扰的时间。", "FlatCat", "原创", CatLine),
            new Quote("一天里最值钱的，是那段不被打断的半小时。", "FlatCat", "原创", CatLine),
            new Quote("别把忙当成有效，忙只是占满了时间。", "FlatCat", "原创", CatLine),
            new Quote("有进度的忙碌才叫努力，其余叫消耗。", "FlatCat", "原创", CatLine),
            new Quote("今天少做一件无关的事，就多一分余量。", "FlatCat", "原创", CatLine),
            new Quote("把「以后再说」改成「现在两分钟」。", "FlatCat", "原创", CatLine),
            new Quote("拖延的解药不是意志力，是把第一步变小。", "FlatCat", "原创", CatLine),
            new Quote("五分钟能做完的事，别让它挂一整天。", "FlatCat", "原创", CatLine),
            new Quote("先收拾桌面，再收拾心情。", "FlatCat", "原创", CatLine),
            new Quote("环境清爽了，脑子也会跟着清爽。", "FlatCat", "原创", CatLine),
            new Quote("少一点多任务，多一点一件事。", "FlatCat", "原创", CatLine),
            new Quote("同时做三件事，往往三件都做浅了。", "FlatCat", "原创", CatLine),
            new Quote("学会说不，才留得住说是的力气。", "FlatCat", "原创", CatLine),
            new Quote("不是所有消息都需要立刻回。", "FlatCat", "原创", CatLine),
            new Quote("把「我应该」换成「我选择」，压力会小一半。", "FlatCat", "原创", CatLine),
            new Quote("你已经在路上了，别急着看终点。", "FlatCat", "原创", CatLine),
            new Quote("进步是悄悄发生的，别用一天判断一年。", "FlatCat", "原创", CatLine),
            new Quote("重复做对的小事，会变成别人看得见的能力。", "FlatCat", "原创", CatLine),
            new Quote("每天十分钟，一年就是六十小时。", "FlatCat", "原创", CatLine),
            new Quote("记录本身就是一种进步。", "FlatCat", "原创", CatLine),
            new Quote("复盘一次，胜过重复三次。", "FlatCat", "原创", CatLine),
            new Quote("犯了错就修，别在自责里多待一秒。", "FlatCat", "原创", CatLine),
            new Quote("把失败当数据，它就不那么疼了。", "FlatCat", "原创", CatLine),
            new Quote("被否定的是方案，不是你这个人。", "FlatCat", "原创", CatLine),
            new Quote("不必向所有人解释你的选择。", "FlatCat", "原创", CatLine),
            new Quote("你的节奏不必和任何人同步。", "FlatCat", "原创", CatLine),
            new Quote("别人在赶路，你也可以在种花。", "FlatCat", "原创", CatLine),
            new Quote("和昨天的自己比，是唯一公平的比较。", "FlatCat", "原创", CatLine),
            new Quote("慢，也是一种到达。", "FlatCat", "原创", CatLine),
            new Quote("稳一点，才走得远。", "FlatCat", "原创", CatLine),
            new Quote("把身体照顾好，才有力气谈理想。", "FlatCat", "原创", CatLine),
            new Quote("起身走两分钟，思路常常就通了。", "FlatCat", "原创", CatLine),
            new Quote("肩膀放松，呼吸放慢，事情没那么急。", "FlatCat", "原创", CatLine),
            new Quote("深呼吸三次，再决定要不要发那条消息。", "FlatCat", "原创", CatLine),
            new Quote("心情不好时，先去做件具体的事。", "FlatCat", "原创", CatLine),
            new Quote("照顾好自己，是效率的前提，不是奖励。", "FlatCat", "原创", CatLine),
            new Quote("今天也要记得吃饭。", "FlatCat", "原创", CatLine),
            new Quote("天塌不下来，先把这杯水喝完。", "FlatCat", "原创", CatLine),
            new Quote("把窗开一条缝，房间和脑子都会透气。", "FlatCat", "原创", CatLine),
            new Quote("晒太阳十分钟，比刷十分钟手机提神。", "FlatCat", "原创", CatLine),
            new Quote("别在深夜做重要决定。", "FlatCat", "原创", CatLine),
            new Quote("明天的你，会感谢今晚早睡的你。", "FlatCat", "原创", CatLine),
            new Quote("把今天收好，明天再打开。", "FlatCat", "原创", CatLine),
            new Quote("完成比完美更接近自由。", "FlatCat", "原创", CatLine),
            new Quote("你已经做得比你以为的多。", "FlatCat", "原创", CatLine),
            new Quote("允许休息，也是本事。", "FlatCat", "原创", CatLine),
        };

        /// <summary>按分类位组装抽句池：内置条目 + 用户自定义条目。</summary>
        public static List<Quote> BuildPool(int mask, List<CustomQuote> custom)
        {
            if ((mask & CatAll) == 0) mask = CatAll;
            List<Quote> pool = new List<Quote>();
            for (int i = 0; i < All.Length; i++)
                if ((All[i].Cat & mask) != 0) pool.Add(All[i]);
            if (custom != null && (mask & CatCustom) != 0)
            {
                for (int i = 0; i < custom.Count && pool.Count < 20000; i++)
                {
                    CustomQuote c = custom[i];
                    if (c == null || string.IsNullOrEmpty(c.Text)) continue;
                    pool.Add(new Quote(c.Text, c.Author, c.Source, CatCustom));
                }
            }
            return pool;
        }

        public static int CountOf(int mask, List<CustomQuote> custom)
        {
            if ((mask & CatAll) == 0) mask = CatAll;
            int n = 0;
            for (int i = 0; i < All.Length; i++) if ((All[i].Cat & mask) != 0) n++;
            if (custom != null && (mask & CatCustom) != 0)
            {
                for (int i = 0; i < custom.Count; i++)
                    if (custom[i] != null && !string.IsNullOrEmpty(custom[i].Text)) n++;
            }
            return n;
        }

        /// <summary>从池子里随机取一条；尽量避开上一条，避免连着重复。</summary>
        public static Quote Pick(List<Quote> pool, string lastText, Random r)
        {
            if (pool == null || pool.Count == 0) return null;
            if (r == null) r = new Random();
            int pick = r.Next(pool.Count);
            for (int guard = 0; guard < 6 && pool.Count > 1 && pool[pick].Text == lastText; guard++)
                pick = r.Next(pool.Count);
            return pool[pick];
        }

        /// <summary>分类名，供设置页与自检显示。</summary>
        public static string CatName(int cat)
        {
            if (cat == CatThought) return "哲思";
            if (cat == CatPoem) return "诗句";
            if (cat == CatLine) return "金句";
            if (cat == CatCustom) return "自定义";
            return "未知";
        }
    }
}
